#!/usr/bin/env bash
set -euo pipefail

# End-to-end SNMP pipeline validation for a configured ATSPM device.
# 1) Reads device + config from ATSPM-Config.
# 2) Runs SNMP OID bundle check.
# 3) Temporarily switches DeviceConfiguration to SNMP mode for EventLogUtility.
# 4) Runs EventLogUtility with -tp Snmp.
# 5) Restores original DeviceConfiguration fields automatically.

DEVICE_IDENTIFIER="${1:-}"
COMMUNITY="${2:-public}"
SNMP_VERSION="${3:-1}"
OIDS_CSV="${4:-1.3.6.1.2.1.1.1.0,1.3.6.1.2.1.1.5.0}"
NETWORK_NAME="${5:-atspm_default}"
SNMP_TIMEOUT_SECS="${6:-2}"
SNMP_RETRIES="${7:-1}"

if [[ -z "${DEVICE_IDENTIFIER}" ]]; then
  echo "Usage: $0 <device-identifier> [community] [snmp-version] [oids-csv] [docker-network] [timeout-seconds] [retries]"
  echo "Example: $0 dixonm60 public 1 '1.3.6.1.2.1.1.1.0,1.3.6.1.2.1.1.5.0' atspm_default 2 1"
  exit 2
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPORT_DIR="${ROOT_DIR}/artifacts/ntcip-reports"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
REPORT_FILE="${REPORT_DIR}/snmp-pipeline-${DEVICE_IDENTIFIER}-${TIMESTAMP}.txt"
mkdir -p "${REPORT_DIR}"

sql_escape() {
  local raw="$1"
  printf "%s" "${raw//\'/\'\'}"
}

CONFIG_UPDATED=0
CONFIG_ID=""
ORIG_PROTOCOL=""
ORIG_PORT=""
ORIG_PATH=""
ORIG_QUERY=""
ORIG_USERNAME=""
ORIG_CONNPROPS=""
ORIG_DECODERS=""
DEVICE_ID=""

restore_config() {
  if [[ ${CONFIG_UPDATED} -eq 1 && -n "${CONFIG_ID}" ]]; then
    local protocol_esc path_esc query_esc user_esc conn_esc decoders_esc
    protocol_esc="$(sql_escape "${ORIG_PROTOCOL}")"
    path_esc="$(sql_escape "${ORIG_PATH}")"
    query_esc="$(sql_escape "${ORIG_QUERY}")"
    user_esc="$(sql_escape "${ORIG_USERNAME}")"
    conn_esc="$(sql_escape "${ORIG_CONNPROPS}")"
    decoders_esc="$(sql_escape "${ORIG_DECODERS}")"

    docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-Config" -c \
      "UPDATE \"DeviceConfigurations\" SET \"Protocol\"='${protocol_esc}', \"Port\"=${ORIG_PORT}, \"Path\"='${path_esc}', \"Query\"='${query_esc}', \"Decoders\"='${decoders_esc}', \"UserName\"='${user_esc}', \"ConnectionProperties\"='${conn_esc}' WHERE \"Id\"=${CONFIG_ID};" >/dev/null
    echo "[INFO] Restored original DeviceConfiguration Id=${CONFIG_ID}." | tee -a "${REPORT_FILE}"
  fi
}

trap restore_config EXIT

echo "SNMP Pipeline Validation Report" > "${REPORT_FILE}"
echo "Generated(UTC): ${TIMESTAMP}" >> "${REPORT_FILE}"
echo "DeviceIdentifier: ${DEVICE_IDENTIFIER}" >> "${REPORT_FILE}"
echo "RequestedCommunity: ${COMMUNITY}" >> "${REPORT_FILE}"
echo "RequestedVersion: ${SNMP_VERSION}" >> "${REPORT_FILE}"
echo "RequestedOids: ${OIDS_CSV}" >> "${REPORT_FILE}"
echo >> "${REPORT_FILE}"

echo "[STEP 1/4] Loading device and configuration metadata..." | tee -a "${REPORT_FILE}"
ROW="$(docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-Config" -t -A -F '|' -c "SELECT d.\"Id\", d.\"Ipaddress\", d.\"DeviceConfigurationId\", dc.\"Protocol\", dc.\"Port\", COALESCE(dc.\"Path\",''), COALESCE(dc.\"Query\",'[]'), COALESCE(dc.\"Decoders\",'[]'), COALESCE(dc.\"UserName\",''), COALESCE(dc.\"ConnectionProperties\",'') FROM \"Devices\" d JOIN \"DeviceConfigurations\" dc ON dc.\"Id\"=d.\"DeviceConfigurationId\" WHERE d.\"DeviceIdentifier\"='${DEVICE_IDENTIFIER}' LIMIT 1;")"

if [[ -z "${ROW//[[:space:]]/}" ]]; then
  echo "[FAIL] Device '${DEVICE_IDENTIFIER}' not found in ATSPM-Config." | tee -a "${REPORT_FILE}"
  exit 1
fi

IFS='|' read -r DEVICE_ID TARGET_IP CONFIG_ID ORIG_PROTOCOL ORIG_PORT ORIG_PATH ORIG_QUERY ORIG_DECODERS ORIG_USERNAME ORIG_CONNPROPS <<< "${ROW}"
echo "[INFO] DeviceId=${DEVICE_ID}, TargetIP=${TARGET_IP}, ConfigId=${CONFIG_ID}, OriginalProtocol=${ORIG_PROTOCOL}" | tee -a "${REPORT_FILE}"

echo "[STEP 2/4] Verifying SNMP connectivity and OID bundle..." | tee -a "${REPORT_FILE}"
if ! "${ROOT_DIR}/scripts/check-ntcip-oid-bundle.sh" "${TARGET_IP}" "${COMMUNITY}" "${SNMP_VERSION}" "${NETWORK_NAME}" "${SNMP_TIMEOUT_SECS}" "${SNMP_RETRIES}" "${OIDS_CSV}" >> "${REPORT_FILE}" 2>&1; then
  echo "[FAIL] OID bundle validation failed. See report for details." | tee -a "${REPORT_FILE}"
  exit 1
fi

echo "[STEP 3/4] Applying temporary SNMP DeviceConfiguration..." | tee -a "${REPORT_FILE}"
SNMP_QUERY_JSON="[$(echo "${OIDS_CSV}" | awk -F',' '{for(i=1;i<=NF;i++){gsub(/^ +| +$/, "", $i); printf "%s\"%s\"", (i>1?",":""), $i}}')]"
CONN_PROPS_JSON="{\"Version\":\"${SNMP_VERSION}\",\"Community\":\"${COMMUNITY}\"}"

docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-Config" -c \
  "UPDATE \"DeviceConfigurations\" SET \"Protocol\"='Snmp', \"Port\"=161, \"Path\"='/', \"Query\"='${SNMP_QUERY_JSON}', \"Decoders\"='[\"SnmpToIndianaDecoder\"]', \"UserName\"='${COMMUNITY}', \"ConnectionProperties\"='${CONN_PROPS_JSON}' WHERE \"Id\"=${CONFIG_ID};" >> "${REPORT_FILE}" 2>&1
CONFIG_UPDATED=1

echo "[STEP 4/4] Running EventLogUtility SNMP download workflow..." | tee -a "${REPORT_FILE}"
docker compose -f "${ROOT_DIR}/docker-compose.yml" build eventlogutility >/dev/null
if ! docker compose -f "${ROOT_DIR}/docker-compose.yml" run --rm --no-deps eventlogutility log false false false -id "${DEVICE_IDENTIFIER}" -tp Snmp >> "${REPORT_FILE}" 2>&1; then
  echo "[FAIL] EventLogUtility returned a non-zero exit code." | tee -a "${REPORT_FILE}"
  exit 1
fi

if ! grep -Eq 'files found on' "${REPORT_FILE}"; then
  echo "[FAIL] No 'files found on' marker detected in EventLogUtility output." | tee -a "${REPORT_FILE}"
  exit 1
fi

if ! grep -Eq 'Downloaded [0-9]+/[0-9]+ resources' "${REPORT_FILE}"; then
  echo "[FAIL] No 'Downloaded X/X resources' marker detected in EventLogUtility output." | tee -a "${REPORT_FILE}"
  exit 1
fi

echo "[STEP 5/5] Verifying persisted event rows in ATSPM-EventLogs..." | tee -a "${REPORT_FILE}"
PERSISTED_ROWS="$(docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-EventLogs" -t -A -c "SELECT COUNT(*) FROM \"CompressedEvents\" WHERE \"DeviceId\"=${DEVICE_ID};")"
PERSISTED_ROWS="$(echo "${PERSISTED_ROWS}" | tr -d '[:space:]')"

if [[ -z "${PERSISTED_ROWS}" ]]; then
  PERSISTED_ROWS="0"
fi

echo "[INFO] CompressedEvents rows for DeviceId=${DEVICE_ID}: ${PERSISTED_ROWS}" | tee -a "${REPORT_FILE}"

if [[ "${PERSISTED_ROWS}" == "0" ]]; then
  echo "[FAIL] No persisted CompressedEvents rows were found for this device." | tee -a "${REPORT_FILE}"
  exit 1
fi

echo "[PASS] SNMP pipeline validation succeeded. Report: ${REPORT_FILE}" | tee -a "${REPORT_FILE}"
exit 0
#!/usr/bin/env bash
set -euo pipefail

# NTCIP/SNMP OID bundle test from ATSPM docker network.
# 1) Checks UDP 161 reachability.
# 2) Auto-discovers working SNMP version/community (unless provided).
# 3) Queries a bundle of OIDs and prints pass/fail per OID.

TARGET_IP="${1:-}"
COMMUNITY="${2:-auto}"
SNMP_VERSION="${3:-auto}"
NETWORK_NAME="${4:-atspm_default}"
SNMP_TIMEOUT_SECS="${5:-2}"
SNMP_RETRIES="${6:-1}"
OIDS_CSV="${7:-}"

if [[ -z "${TARGET_IP}" ]]; then
  echo "Usage: $0 <target-ip> [community|auto] [snmp-version|auto] [docker-network] [timeout-seconds] [retries] [oids-csv]"
  echo "Example: $0 166.156.88.223 auto auto atspm_default 2 1"
  echo "Example with custom OIDs: $0 166.156.88.223 public 1 atspm_default 2 1 '1.3.6.1.2.1.1.1.0,1.3.6.1.2.1.1.3.0'"
  exit 2
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -n "${OIDS_CSV}" ]]; then
  IFS=',' read -r -a OIDS <<< "${OIDS_CSV}"
else
  OIDS=(
    "1.3.6.1.2.1.1.1.0"  # sysDescr
    "1.3.6.1.2.1.1.3.0"  # sysUpTime
    "1.3.6.1.2.1.1.5.0"  # sysName
    "1.3.6.1.2.1.1.6.0"  # sysLocation
    "1.3.6.1.2.1.1.4.0"  # sysContact
  )
fi

snmp_get() {
  local version="$1"
  local community="$2"
  local oid="$3"

  docker run --rm --network "${NETWORK_NAME}" alpine:3.20 sh -lc \
    "apk add --no-cache net-snmp-tools >/dev/null && snmpget -v${version} -c '${community}' -t ${SNMP_TIMEOUT_SECS} -r ${SNMP_RETRIES} ${TARGET_IP} ${oid}" 2>&1 || true
}

echo "[STEP 1/3] Probing UDP 161 reachability..."
"${ROOT_DIR}/scripts/check-ntcip-reachability.sh" "${TARGET_IP}" "161" "${NETWORK_NAME}"

echo "[STEP 2/3] Discovering working SNMP settings..."

if [[ "${SNMP_VERSION}" == "auto" ]]; then
  VERSION_LIST=(2c 1)
else
  VERSION_LIST=("${SNMP_VERSION}")
fi

if [[ "${COMMUNITY}" == "auto" ]]; then
  COMMUNITY_LIST=(public private Public Private)
else
  COMMUNITY_LIST=("${COMMUNITY}")
fi

FOUND=0
FOUND_VERSION=""
FOUND_COMMUNITY=""
PROBE_OID="1.3.6.1.2.1.1.1.0"

for v in "${VERSION_LIST[@]}"; do
  for c in "${COMMUNITY_LIST[@]}"; do
    echo "[TRY] version=${v}, community=${c}, oid=${PROBE_OID}"
    OUT="$(snmp_get "${v}" "${c}" "${PROBE_OID}")"
    echo "${OUT}"

    if echo "${OUT}" | grep -Eq "= (STRING|INTEGER|OID|Hex-STRING|Timeticks|Counter32|Counter64|Gauge32|IpAddress)"; then
      FOUND=1
      FOUND_VERSION="${v}"
      FOUND_COMMUNITY="${c}"
      break 2
    fi
  done
done

if [[ ${FOUND} -ne 1 ]]; then
  echo "[FAIL] Could not discover working SNMP settings."
  exit 1
fi

echo "[PASS] Using version=${FOUND_VERSION}, community=${FOUND_COMMUNITY}."

echo "[STEP 3/3] Querying OID bundle..."
PASS_COUNT=0
FAIL_COUNT=0

for oid in "${OIDS[@]}"; do
  OID_TRIMMED="$(echo "${oid}" | xargs)"
  OUT="$(snmp_get "${FOUND_VERSION}" "${FOUND_COMMUNITY}" "${OID_TRIMMED}")"

  if echo "${OUT}" | grep -Eq "= (STRING|INTEGER|OID|Hex-STRING|Timeticks|Counter32|Counter64|Gauge32|IpAddress)"; then
    echo "[PASS] ${OID_TRIMMED} -> $(echo "${OUT}" | head -n 1)"
    PASS_COUNT=$((PASS_COUNT + 1))
  else
    echo "[FAIL] ${OID_TRIMMED} -> $(echo "${OUT}" | head -n 1)"
    FAIL_COUNT=$((FAIL_COUNT + 1))
  fi
done

echo "[SUMMARY] pass=${PASS_COUNT}, fail=${FAIL_COUNT}, total=$((PASS_COUNT + FAIL_COUNT))"

if [[ ${FAIL_COUNT} -gt 0 ]]; then
  exit 3
fi

exit 0
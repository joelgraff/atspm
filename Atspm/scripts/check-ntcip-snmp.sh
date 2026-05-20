#!/usr/bin/env bash
set -euo pipefail

# NTCIP/SNMP connectivity check from the ATSPM docker network.
# Steps:
# 1) Probe UDP port reachability.
# 2) Execute an SNMP GET against a test OID.

TARGET_IP="${1:-}"
COMMUNITY="${2:-auto}"
OID="${3:-1.3.6.1.2.1.1.1.0}" # sysDescr.0
UDP_PORT="${4:-161}"
NETWORK_NAME="${5:-atspm_default}"
SNMP_VERSION="${6:-auto}"
SNMP_TIMEOUT_SECS="${7:-2}"
SNMP_RETRIES="${8:-1}"

if [[ -z "${TARGET_IP}" ]]; then
  echo "Usage: $0 <target-ip> [community] [oid] [udp-port] [docker-network] [snmp-version] [timeout-seconds] [retries]"
  echo "Example: $0 166.156.88.223 auto 1.3.6.1.2.1.1.1.0 161 atspm_default auto 2 1"
  exit 2
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "[STEP 1/2] Probing UDP ${UDP_PORT} reachability..."
"${ROOT_DIR}/scripts/check-ntcip-reachability.sh" "${TARGET_IP}" "${UDP_PORT}" "${NETWORK_NAME}"

echo "[STEP 2/2] Running SNMP GET for OID ${OID}..."

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

SNMP_OUTPUT=""
SUCCESS=0

for v in "${VERSION_LIST[@]}"; do
  for c in "${COMMUNITY_LIST[@]}"; do
    echo "[TRY] snmpget -v${v} -c ${c} ${TARGET_IP} ${OID}"
    ATTEMPT_OUTPUT="$(docker run --rm --network "${NETWORK_NAME}" alpine:3.20 sh -lc "apk add --no-cache net-snmp-tools >/dev/null && snmpget -v${v} -c '${c}' -t ${SNMP_TIMEOUT_SECS} -r ${SNMP_RETRIES} ${TARGET_IP} ${OID}" 2>&1 || true)"
    echo "${ATTEMPT_OUTPUT}"

    if echo "${ATTEMPT_OUTPUT}" | grep -Eq "= (STRING|INTEGER|OID|Hex-STRING|Timeticks|Counter32|Counter64|Gauge32|IpAddress)"; then
      echo "[PASS] SNMP GET succeeded with version=${v}, community=${c}."
      SUCCESS=1
      SNMP_OUTPUT="${ATTEMPT_OUTPUT}"
      break 2
    fi

    SNMP_OUTPUT="${ATTEMPT_OUTPUT}"
  done
done

if [[ ${SUCCESS} -eq 1 ]]; then
  exit 0
fi

if echo "${SNMP_OUTPUT}" | grep -Eq "Timeout: No Response"; then
  echo "[FAIL] UDP ${UDP_PORT} is reachable, but SNMP GET timed out."
  echo "[HINT] Check SNMP community/security settings and modem forwarding for SNMP payloads."
  exit 1
fi

if echo "${SNMP_OUTPUT}" | grep -Eqi "Authentication failure|authorizationError|noAccess"; then
  echo "[FAIL] SNMP agent responded but rejected credentials/permissions."
  exit 1
fi

echo "[WARN] SNMP result was inconclusive."
exit 3
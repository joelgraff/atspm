#!/usr/bin/env bash
set -euo pipefail

# Tests UDP reachability (default SNMP/NTCIP port 161) from the same Docker network
# used by the ATSPM stack.

TARGET_IP="${1:-}"
TARGET_PORT="${2:-161}"
NETWORK_NAME="${3:-atspm_default}"

if [[ -z "${TARGET_IP}" ]]; then
  echo "Usage: $0 <target-ip> [udp-port] [docker-network]"
  echo "Example: $0 166.156.88.223 161 atspm_default"
  exit 2
fi

echo "[INFO] Probing UDP ${TARGET_PORT} on ${TARGET_IP} from Docker network ${NETWORK_NAME}..."

SCAN_OUTPUT="$(docker run --rm --network "${NETWORK_NAME}" instrumentisto/nmap -sU -Pn -p "${TARGET_PORT}" --reason "${TARGET_IP}" 2>&1)"
echo "${SCAN_OUTPUT}"

if echo "${SCAN_OUTPUT}" | grep -Eq "${TARGET_PORT}/udp +open"; then
  echo "[PASS] UDP ${TARGET_PORT} is reachable from ATSPM Docker network."
  exit 0
fi

if echo "${SCAN_OUTPUT}" | grep -Eq "${TARGET_PORT}/udp +open\|filtered|${TARGET_PORT}/udp +filtered"; then
  echo "[WARN] UDP ${TARGET_PORT} is not definitively open (filtered/open|filtered)."
  exit 3
fi

echo "[FAIL] UDP ${TARGET_PORT} is not reachable from ATSPM Docker network."
exit 1
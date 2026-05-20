#!/usr/bin/env bash
set -euo pipefail

# Generates a timestamped NTCIP/SNMP validation report file.

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
  exit 2
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPORT_DIR="${ROOT_DIR}/artifacts/ntcip-reports"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
REPORT_FILE="${REPORT_DIR}/ntcip-report-${TARGET_IP//./-}-${TIMESTAMP}.txt"

mkdir -p "${REPORT_DIR}"

{
  echo "ATSPM NTCIP/SNMP Validation Report"
  echo "Generated(UTC): ${TIMESTAMP}"
  echo "TargetIP: ${TARGET_IP}"
  echo "Network: ${NETWORK_NAME}"
  echo "RequestedCommunity: ${COMMUNITY}"
  echo "RequestedVersion: ${SNMP_VERSION}"
  echo "TimeoutSeconds: ${SNMP_TIMEOUT_SECS}"
  echo "Retries: ${SNMP_RETRIES}"
  if [[ -n "${OIDS_CSV}" ]]; then
    echo "CustomOids: ${OIDS_CSV}"
  else
    echo "CustomOids: <default bundle>"
  fi
  echo
  echo "=== Reachability Check ==="
} > "${REPORT_FILE}"

if "${ROOT_DIR}/scripts/check-ntcip-reachability.sh" "${TARGET_IP}" 161 "${NETWORK_NAME}" >> "${REPORT_FILE}" 2>&1; then
  REACH_STATUS="PASS"
else
  REACH_STATUS="FAIL"
fi

{
  echo
  echo "=== SNMP OID Bundle Check ==="
} >> "${REPORT_FILE}"

if "${ROOT_DIR}/scripts/check-ntcip-oid-bundle.sh" "${TARGET_IP}" "${COMMUNITY}" "${SNMP_VERSION}" "${NETWORK_NAME}" "${SNMP_TIMEOUT_SECS}" "${SNMP_RETRIES}" "${OIDS_CSV}" >> "${REPORT_FILE}" 2>&1; then
  SNMP_STATUS="PASS"
else
  SNMP_STATUS="FAIL"
fi

{
  echo
  echo "=== Summary ==="
  echo "Reachability: ${REACH_STATUS}"
  echo "SNMPBundle: ${SNMP_STATUS}"
} >> "${REPORT_FILE}"

echo "[INFO] Report written to ${REPORT_FILE}"

if [[ "${REACH_STATUS}" == "PASS" && "${SNMP_STATUS}" == "PASS" ]]; then
  echo "[PASS] NTCIP/SNMP validation complete."
  exit 0
fi

echo "[WARN] Validation completed with failures. See report for details."
exit 1
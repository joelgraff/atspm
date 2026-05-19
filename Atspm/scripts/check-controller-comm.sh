#!/usr/bin/env bash
set -euo pipefail

# Verifies ATSPM can communicate with a configured controller by running EventLogUtility.
# Success criteria:
# 1) EventLogUtility starts and completes.
# 2) It reports at least one file discovered on the controller.
# 3) It reports download progress/results.

DEVICE_IDENTIFIER="${1:-}"
TRANSPORT_PROTOCOL="${2:-Http}"
PING_BEFORE_DOWNLOAD="${3:-false}"

if [[ -z "${DEVICE_IDENTIFIER}" ]]; then
  echo "Usage: $0 <device-identifier> [Ftp|Http|Sftp|Snmp] [true|false]"
  echo "Example: $0 dixonm60 Http false"
  exit 2
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TMP_LOG="$(mktemp)"
trap 'rm -f "${TMP_LOG}"' EXIT

echo "[INFO] Building EventLogUtility image..."
docker compose -f "${ROOT_DIR}/docker-compose.yml" build eventlogutility >/dev/null

echo "[INFO] Running controller communication check for device '${DEVICE_IDENTIFIER}' using protocol '${TRANSPORT_PROTOCOL}'..."
set +e
docker compose -f "${ROOT_DIR}/docker-compose.yml" run --rm --no-deps eventlogutility \
  log false false "${PING_BEFORE_DOWNLOAD}" \
  -id "${DEVICE_IDENTIFIER}" \
  -tp "${TRANSPORT_PROTOCOL}" | tee "${TMP_LOG}"
CMD_EXIT=$?
set -e

if [[ ${CMD_EXIT} -ne 0 ]]; then
  echo "[FAIL] EventLogUtility exited with code ${CMD_EXIT}."
  exit ${CMD_EXIT}
fi

if ! grep -Eq 'files found on' "${TMP_LOG}"; then
  echo "[FAIL] No 'files found on' message was detected. ATSPM may not be reaching the controller or the configured path returned no files."
  exit 1
fi

if ! grep -Eq 'Downloaded [0-9]+/[0-9]+ resources' "${TMP_LOG}"; then
  echo "[FAIL] No download completion message was detected."
  exit 1
fi

echo "[PASS] ATSPM successfully connected to '${DEVICE_IDENTIFIER}' and transferred controller data."
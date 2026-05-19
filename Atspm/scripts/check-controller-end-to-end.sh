#!/usr/bin/env bash
set -euo pipefail

# End-to-end ATSPM controller validation:
# 1) Communication check via EventLogUtility run.
# 2) DeviceId lookup in ATSPM-Config.
# 3) Persisted event check in ATSPM-EventLogs.CompressedEvents.

DEVICE_IDENTIFIER="${1:-}"
TRANSPORT_PROTOCOL="${2:-Http}"
PING_BEFORE_DOWNLOAD="${3:-false}"

if [[ -z "${DEVICE_IDENTIFIER}" ]]; then
  echo "Usage: $0 <device-identifier> [Ftp|Http|Sftp|Snmp] [true|false]"
  echo "Example: $0 dixonm60 Http false"
  exit 2
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "[STEP 1/3] Verifying controller communication..."
"${ROOT_DIR}/scripts/check-controller-comm.sh" "${DEVICE_IDENTIFIER}" "${TRANSPORT_PROTOCOL}" "${PING_BEFORE_DOWNLOAD}"

echo "[STEP 2/3] Resolving numeric DeviceId from ATSPM-Config..."
DEVICE_ID_RAW="$(docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-Config" -t -A -c "SELECT \"Id\" FROM \"Devices\" WHERE \"DeviceIdentifier\"='${DEVICE_IDENTIFIER}' ORDER BY \"Id\" DESC LIMIT 1;")"
DEVICE_ID="$(echo "${DEVICE_ID_RAW}" | tr -d '[:space:]')"
DEVICE_CONFIGURATION_ID_RAW="$(docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-Config" -t -A -c "SELECT d.\"DeviceConfigurationId\" FROM \"Devices\" d WHERE d.\"Id\"=${DEVICE_ID} LIMIT 1;")"
DEVICE_CONFIGURATION_ID="$(echo "${DEVICE_CONFIGURATION_ID_RAW}" | tr -d '[:space:]')"
DECODERS_RAW="$(docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-Config" -t -A -c "SELECT COALESCE(dc.\"Decoders\"::text,'[]') FROM \"DeviceConfigurations\" dc WHERE dc.\"Id\"=${DEVICE_CONFIGURATION_ID} LIMIT 1;")"
DECODERS="$(echo "${DECODERS_RAW}" | tr -d '[:space:]')"
PATH_RAW="$(docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-Config" -t -A -c "SELECT COALESCE(dc.\"Path\",'') FROM \"DeviceConfigurations\" dc WHERE dc.\"Id\"=${DEVICE_CONFIGURATION_ID} LIMIT 1;")"
PATH_VALUE="$(echo "${PATH_RAW}" | tr -d '\r')"

if [[ -z "${DEVICE_ID}" ]]; then
  echo "[FAIL] Device identifier '${DEVICE_IDENTIFIER}' was not found in ATSPM-Config.Devices."
  exit 1
fi

echo "[INFO] Device '${DEVICE_IDENTIFIER}' maps to DeviceId=${DEVICE_ID}."
echo "[INFO] DeviceConfigurationId=${DEVICE_CONFIGURATION_ID}, Decoders=${DECODERS}."
if [[ "${PATH_VALUE}" =~ ^https?:// ]]; then
  echo "[WARN] DeviceConfiguration.Path is an absolute URL: ${PATH_VALUE}"
  echo "[WARN] For HTTP downloads, ATSPM already uses Device.Ipaddress + DeviceConfiguration.Port as base address."
  echo "[WARN] Absolute URLs in Path can override the configured port and route unexpectedly. Use a relative path like '/'."
fi

echo "[STEP 3/3] Checking persisted compressed events..."
EVENT_SUMMARY="$(docker compose -f "${ROOT_DIR}/docker-compose.yml" exec -T postgres psql -U admin -d "ATSPM-EventLogs" -t -A -F '|' -c "SELECT \"LocationIdentifier\", \"DeviceId\", \"DataType\", COUNT(*)::text AS rows, COALESCE(MAX(\"Start\")::text,''), COALESCE(MAX(\"End\")::text,'') FROM \"CompressedEvents\" WHERE \"DeviceId\"=${DEVICE_ID} GROUP BY \"LocationIdentifier\", \"DeviceId\", \"DataType\" ORDER BY MAX(\"End\") DESC LIMIT 10;")"

if [[ -z "${EVENT_SUMMARY//[[:space:]]/}" ]]; then
  echo "[WARN] No rows found in ATSPM-EventLogs.CompressedEvents for DeviceId=${DEVICE_ID}."
  echo "[WARN] Communication passed, but ingestion evidence is missing. Common causes: decoder mismatch, data type filtering, or archive/save step issues."
  if [[ "${DECODERS}" == "[]" ]]; then
    echo "[HINT] This device configuration has no decoders assigned."
    echo "[HINT] Available built-in decoder names include: AscToIndianaDecoder, MaxtimeToIndianaDecoder"
    echo "[HINT] Example SQL to set one decoder (run only after confirming file format):"
    echo "       UPDATE \"DeviceConfigurations\" SET \"Decoders\"='[\"AscToIndianaDecoder\"]' WHERE \"Id\"=${DEVICE_CONFIGURATION_ID};"
  fi
  exit 3
fi

echo "[PASS] Persisted event rows found for DeviceId=${DEVICE_ID}."
echo "LocationIdentifier|DeviceId|DataType|Rows|LatestStart|LatestEnd"
echo "${EVENT_SUMMARY}"
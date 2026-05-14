#!/usr/bin/env bash
set -euo pipefail

# Quick ingress/service smoke checks.
# Validates key HTTP endpoints through nginx and optional direct API ports.

BASE_URL="${BASE_URL:-https://localhost:3443}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-20}"
CHECK_DIRECT_PORTS="${CHECK_DIRECT_PORTS:-true}"

usage() {
  cat <<'EOF'
Usage: scripts/smoke-services.sh [options]

Options:
  --base-url URL              Base ingress URL (default: https://localhost:3443)
  --timeout SECONDS           curl timeout seconds (default: 20)
  --check-direct-ports BOOL   true/false for direct API checks (default: true)
  -h, --help                  Show this help

Environment overrides:
  BASE_URL, TIMEOUT_SECONDS, CHECK_DIRECT_PORTS
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base-url)
      BASE_URL="$2"
      shift 2
      ;;
    --timeout)
      TIMEOUT_SECONDS="$2"
      shift 2
      ;;
    --check-direct-ports)
      CHECK_DIRECT_PORTS="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage
      exit 1
      ;;
  esac
done

command -v curl >/dev/null 2>&1 || {
  echo "ERROR: curl is required but not installed." >&2
  exit 1
}

say_ok() { printf 'PASS: %s\n' "$1"; }
say_fail() { printf 'FAIL: %s\n' "$1"; }

check_status() {
  local url="$1"
  local expected="$2"
  local label="$3"

  local code
  code="$(curl -k -sS -m "$TIMEOUT_SECONDS" -o /dev/null -w '%{http_code}' "$url")"

  if [[ "$code" == "$expected" ]]; then
    say_ok "$label (expected=$expected, actual=$code)"
    return 0
  fi

  say_fail "$label (expected=$expected, actual=$code)"
  return 1
}

all_ok=true

# End-to-end checks through nginx ingress.
if ! check_status "${BASE_URL}/performance-measures" "200" "WebUI route through nginx"; then
  all_ok=false
fi
if ! check_status "${BASE_URL}/api/config/swagger/index.html" "200" "Config API swagger through nginx"; then
  all_ok=false
fi
if ! check_status "${BASE_URL}/api/data/swagger/index.html" "200" "Data API swagger through nginx"; then
  all_ok=false
fi
if ! check_status "${BASE_URL}/api/report/swagger/index.html" "200" "Report API swagger through nginx"; then
  all_ok=false
fi
if ! check_status "${BASE_URL}/api/identity/swagger/index.html" "200" "Identity API swagger through nginx"; then
  all_ok=false
fi

# Optional direct port checks to isolate nginx vs backend issues quickly.
if [[ "${CHECK_DIRECT_PORTS,,}" == "true" ]]; then
  if ! check_status "https://localhost:44400/swagger/index.html" "200" "Config API direct port"; then
    all_ok=false
  fi
  if ! check_status "https://localhost:44401/swagger/index.html" "200" "Data API direct port"; then
    all_ok=false
  fi
  if ! check_status "https://localhost:44402/swagger/index.html" "200" "Report API direct port"; then
    all_ok=false
  fi
  if ! check_status "https://localhost:44403/swagger/index.html" "200" "Identity API direct port"; then
    all_ok=false
  fi
fi

if [[ "$all_ok" == true ]]; then
  echo "Smoke service check passed."
  exit 0
fi

echo "Smoke service check failed."
exit 1

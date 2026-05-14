#!/usr/bin/env bash
set -euo pipefail

# Runs all local smoke checks and returns a single pass/fail code.
# This script executes:
# - scripts/smoke-services.sh
# - scripts/smoke-auth.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SERVICES_SCRIPT="${ROOT_DIR}/scripts/smoke-services.sh"
AUTH_SCRIPT="${ROOT_DIR}/scripts/smoke-auth.sh"

usage() {
  cat <<'EOF'
Usage: scripts/smoke-all.sh [options]

Options:
  --skip-services       Skip services smoke checks
  --skip-auth           Skip auth smoke checks
  -h, --help            Show this help

Notes:
  - Any environment variables consumed by the child scripts are honored.
  - Exit code is non-zero if any selected suite fails.
EOF
}

RUN_SERVICES=true
RUN_AUTH=true

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-services)
      RUN_SERVICES=false
      shift
      ;;
    --skip-auth)
      RUN_AUTH=false
      shift
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

if [[ "$RUN_SERVICES" == false && "$RUN_AUTH" == false ]]; then
  echo "Nothing to run: both suites are skipped."
  exit 1
fi

if [[ "$RUN_SERVICES" == true && ! -x "$SERVICES_SCRIPT" ]]; then
  echo "ERROR: Missing or non-executable script: $SERVICES_SCRIPT" >&2
  exit 1
fi

if [[ "$RUN_AUTH" == true && ! -x "$AUTH_SCRIPT" ]]; then
  echo "ERROR: Missing or non-executable script: $AUTH_SCRIPT" >&2
  exit 1
fi

overall_ok=true

if [[ "$RUN_SERVICES" == true ]]; then
  echo "Running services smoke checks..."
  if "$SERVICES_SCRIPT"; then
    echo "Services smoke suite: PASS"
  else
    echo "Services smoke suite: FAIL"
    overall_ok=false
  fi
fi

if [[ "$RUN_AUTH" == true ]]; then
  echo "Running auth smoke checks..."
  if "$AUTH_SCRIPT"; then
    echo "Auth smoke suite: PASS"
  else
    echo "Auth smoke suite: FAIL"
    overall_ok=false
  fi
fi

if [[ "$overall_ok" == true ]]; then
  echo "All requested smoke suites passed."
  exit 0
fi

echo "One or more smoke suites failed."
exit 1

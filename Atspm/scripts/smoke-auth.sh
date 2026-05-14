#!/usr/bin/env bash
set -euo pipefail

# Quick end-to-end smoke checks for ingress + auth.
# Validates:
# 1) Public login page through nginx
# 2) Identity login
# 3) Protected endpoint rejects missing/invalid token
# 4) Protected endpoint accepts valid admin token

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"

BASE_URL="${BASE_URL:-https://localhost:3443}"
LOGIN_PATH="${LOGIN_PATH:-/api/identity/api/v1/Account/login}"
PROTECTED_PATH="${PROTECTED_PATH:-/api/identity/api/v1/Users}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-20}"

ADMIN_EMAIL="${ADMIN_EMAIL:-}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-}"

usage() {
  cat <<'EOF'
Usage: scripts/smoke-auth.sh [options]

Options:
  --base-url URL           Base ingress URL (default: https://localhost:3443)
  --email EMAIL            Admin email (default: from .env ADMIN_EMAIL)
  --password PASSWORD      Admin password (default: from .env ADMIN_PASSWORD)
  --protected-path PATH    Protected endpoint path (default: /api/identity/api/v1/Users)
  --timeout SECONDS        curl timeout seconds (default: 20)
  -h, --help               Show this help

Environment overrides:
  BASE_URL, ADMIN_EMAIL, ADMIN_PASSWORD, PROTECTED_PATH, TIMEOUT_SECONDS
EOF
}

read_env_value() {
  local key="$1"
  local file="$2"
  [[ -f "$file" ]] || return 1
  local line
  line="$(grep -E "^${key}=" "$file" | tail -n 1 || true)"
  [[ -n "$line" ]] || return 1
  printf '%s' "${line#*=}"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base-url)
      BASE_URL="$2"
      shift 2
      ;;
    --email)
      ADMIN_EMAIL="$2"
      shift 2
      ;;
    --password)
      ADMIN_PASSWORD="$2"
      shift 2
      ;;
    --protected-path)
      PROTECTED_PATH="$2"
      shift 2
      ;;
    --timeout)
      TIMEOUT_SECONDS="$2"
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

if [[ -z "$ADMIN_EMAIL" ]]; then
  ADMIN_EMAIL="$(read_env_value "ADMIN_EMAIL" "$ENV_FILE" || true)"
fi
if [[ -z "$ADMIN_PASSWORD" ]]; then
  ADMIN_PASSWORD="$(read_env_value "ADMIN_PASSWORD" "$ENV_FILE" || true)"
fi

if [[ -z "$ADMIN_EMAIL" || -z "$ADMIN_PASSWORD" ]]; then
  echo "ERROR: Missing admin credentials. Set ADMIN_EMAIL/ADMIN_PASSWORD or pass --email/--password." >&2
  exit 1
fi

command -v curl >/dev/null 2>&1 || {
  echo "ERROR: curl is required but not installed." >&2
  exit 1
}

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

LOGIN_JSON="${TMP_DIR}/login.json"
LOGIN_RESP="${TMP_DIR}/login_response.json"

cat >"$LOGIN_JSON" <<EOF
{"email":"${ADMIN_EMAIL}","password":"${ADMIN_PASSWORD}","rememberMe":false}
EOF

say_ok() { printf 'PASS: %s\n' "$1"; }
say_fail() { printf 'FAIL: %s\n' "$1"; }

check_status() {
  local url="$1"
  local expected="$2"
  local label="$3"
  local auth_header="${4:-}"

  local code
  if [[ -n "$auth_header" ]]; then
    code="$(curl -k -sS -m "$TIMEOUT_SECONDS" -o /dev/null -w '%{http_code}' -H "$auth_header" "$url")"
  else
    code="$(curl -k -sS -m "$TIMEOUT_SECONDS" -o /dev/null -w '%{http_code}' "$url")"
  fi

  if [[ "$code" == "$expected" ]]; then
    say_ok "$label (expected=$expected, actual=$code)"
    return 0
  fi

  say_fail "$label (expected=$expected, actual=$code)"
  return 1
}

all_ok=true

if ! check_status "${BASE_URL}/login" "200" "Public login page reachable"; then
  all_ok=false
fi

login_code="$(curl -k -sS -m "$TIMEOUT_SECONDS" -o "$LOGIN_RESP" -w '%{http_code}' -H 'Content-Type: application/json' --data-binary "@$LOGIN_JSON" "${BASE_URL}${LOGIN_PATH}")"
if [[ "$login_code" == "200" ]]; then
  say_ok "Identity login succeeded"
else
  say_fail "Identity login failed (expected=200, actual=${login_code})"
  all_ok=false
fi

token="$(sed -n 's/.*"token":"\([^"]*\)".*/\1/p' "$LOGIN_RESP")"
if [[ -n "$token" ]]; then
  say_ok "JWT token returned"
else
  say_fail "No JWT token found in login response"
  all_ok=false
fi

if ! check_status "${BASE_URL}${PROTECTED_PATH}" "401" "Protected endpoint rejects missing token"; then
  all_ok=false
fi

if ! check_status "${BASE_URL}${PROTECTED_PATH}" "401" "Protected endpoint rejects invalid token" "Authorization: Bearer invalid.token.value"; then
  all_ok=false
fi

if [[ -n "$token" ]]; then
  if ! check_status "${BASE_URL}${PROTECTED_PATH}" "200" "Protected endpoint accepts valid admin token" "Authorization: Bearer ${token}"; then
    all_ok=false
  fi
fi

if [[ "$all_ok" == true ]]; then
  echo "Smoke auth check passed."
  exit 0
fi

echo "Smoke auth check failed."
exit 1

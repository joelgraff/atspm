#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   scripts/compose-profile.sh safe build
#   scripts/compose-profile.sh safe up
#   scripts/compose-profile.sh fast rebuild-up
#
# Profiles:
#   safe -> lower desktop impact, default parallel limit 2, lower CPU/IO priority
#   fast -> higher throughput, default parallel limit 3

PROFILE="${1:-safe}"
ACTION="${2:-up}"

case "${PROFILE}" in
  safe)
    PARALLEL_LIMIT="${COMPOSE_PARALLEL_LIMIT:-2}"
    PREFIX=(ionice -c2 -n7 nice -n 10)
    ;;
  fast)
    PARALLEL_LIMIT="${COMPOSE_PARALLEL_LIMIT:-3}"
    PREFIX=()
    ;;
  *)
    echo "Unknown profile: ${PROFILE}"
    echo "Use: safe | fast"
    exit 1
    ;;
esac

run_compose() {
  local subcmd=("$@")
  sg docker -c "COMPOSE_PARALLEL_LIMIT=${PARALLEL_LIMIT} ${PREFIX[*]} docker compose ${subcmd[*]}"
}

echo "Profile=${PROFILE} ParallelLimit=${PARALLEL_LIMIT} Action=${ACTION}"

case "${ACTION}" in
  build)
    run_compose build
    ;;
  up)
    run_compose up -d --no-build
    ;;
  rebuild-up)
    run_compose build
    run_compose up -d --no-build
    ;;
  ps)
    run_compose ps -a
    ;;
  *)
    echo "Unknown action: ${ACTION}"
    echo "Use: build | up | rebuild-up | ps"
    exit 1
    ;;
esac

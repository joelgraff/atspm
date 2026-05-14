#!/usr/bin/env bash
set -euo pipefail

# Relocates containerd persistent storage and Docker/containerd temp files
# off root filesystem onto /home/docker-data.

TARGET_BASE="/home/docker-data"
TARGET_CONTAINERD="${TARGET_BASE}/containerd"
TARGET_TMP="${TARGET_BASE}/tmp"
SOURCE_CONTAINERD="/var/lib/containerd"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run as root: sudo bash scripts/relocate-container-storage.sh"
  exit 1
fi

echo "[1/8] Stopping docker/containerd services..."
systemctl stop docker.service docker.socket containerd.service || true

echo "[2/8] Creating target directories..."
mkdir -p "${TARGET_BASE}" "${TARGET_TMP}"
chmod 1777 "${TARGET_TMP}"

if [[ -L "${SOURCE_CONTAINERD}" ]]; then
  echo "[3/8] ${SOURCE_CONTAINERD} is already a symlink: $(readlink -f "${SOURCE_CONTAINERD}")"
elif [[ -d "${SOURCE_CONTAINERD}" ]]; then
  echo "[3/8] Moving ${SOURCE_CONTAINERD} to ${TARGET_CONTAINERD} ..."
  if [[ -e "${TARGET_CONTAINERD}" ]]; then
    BACKUP_TARGET="${TARGET_CONTAINERD}.pre-migrate-${TIMESTAMP}"
    echo "Existing target found, backing it up to ${BACKUP_TARGET}"
    mv "${TARGET_CONTAINERD}" "${BACKUP_TARGET}"
  fi
  mv "${SOURCE_CONTAINERD}" "${TARGET_CONTAINERD}"
  ln -s "${TARGET_CONTAINERD}" "${SOURCE_CONTAINERD}"
else
  echo "[3/8] Source ${SOURCE_CONTAINERD} not found, creating fresh target and symlink."
  mkdir -p "${TARGET_CONTAINERD}"
  ln -s "${TARGET_CONTAINERD}" "${SOURCE_CONTAINERD}"
fi

echo "[4/8] Writing systemd override for docker TMPDIR..."
mkdir -p /etc/systemd/system/docker.service.d
cat > /etc/systemd/system/docker.service.d/override.conf <<EOF
[Service]
Environment=TMPDIR=${TARGET_TMP}
EOF

echo "[5/8] Writing systemd override for containerd TMPDIR..."
mkdir -p /etc/systemd/system/containerd.service.d
cat > /etc/systemd/system/containerd.service.d/override.conf <<EOF
[Service]
Environment=TMPDIR=${TARGET_TMP}
EOF

echo "[6/8] Reloading systemd and starting services..."
systemctl daemon-reload
systemctl start containerd.service
systemctl start docker.socket docker.service

echo "[7/8] Verifying runtime state..."
systemctl --no-pager --full status containerd.service | sed -n '1,12p' || true
systemctl --no-pager --full status docker.service | sed -n '1,12p' || true

echo "[8/8] Validation outputs..."
echo "Docker root from docker info:"
docker info --format 'DockerRoot={{.DockerRootDir}} Driver={{.Driver}}' || true
echo "containerd symlink target:"
ls -ld "${SOURCE_CONTAINERD}" || true
readlink -f "${SOURCE_CONTAINERD}" || true
echo "Disk usage for root/home/containerd path:"
df -h / /home "${SOURCE_CONTAINERD}" || true

echo "Completed migration."
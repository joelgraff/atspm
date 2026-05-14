#!/usr/bin/env bash
set -euo pipefail

# Linux bootstrap for ATSPM development on Ubuntu 24.04+
# This script installs core prerequisites, .NET 8 SDK, Node.js 20 LTS,
# and Docker Engine packages from Ubuntu repos.

if [[ "${EUID}" -eq 0 ]]; then
  echo "Run this script as a regular user (it will call sudo as needed)."
  exit 1
fi

sudo apt-get update
sudo apt-get install -y \
  ca-certificates \
  curl \
  gnupg \
  lsb-release \
  openssl \
  git \
  jq

# Install .NET 8 SDK
if ! command -v dotnet >/dev/null 2>&1; then
  curl -fsSL https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -o /tmp/packages-microsoft-prod.deb
  sudo dpkg -i /tmp/packages-microsoft-prod.deb
  sudo apt-get update
  sudo apt-get install -y dotnet-sdk-8.0
fi

# Install Node.js 20 LTS
if ! command -v node >/dev/null 2>&1; then
  curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
  sudo apt-get install -y nodejs
fi

# Install Docker Engine + Compose plugin
if ! command -v docker >/dev/null 2>&1; then
  sudo apt-get install -y docker.io docker-compose-v2
  sudo usermod -aG docker "$USER"
  echo "Docker group updated for user '$USER'. Log out and back in before using docker without sudo."
fi

echo
echo "Installed tool versions:"
command -v dotnet >/dev/null 2>&1 && dotnet --version || true
command -v node >/dev/null 2>&1 && node --version || true
command -v npm >/dev/null 2>&1 && npm --version || true
command -v docker >/dev/null 2>&1 && docker --version || true
command -v docker >/dev/null 2>&1 && docker compose version || true

echo
echo "Next steps:"
echo "1) cp .env.example .env"
echo "2) adjust values in .env"
echo "3) generate certs into nginx/certs (see README)"
echo "4) dotnet restore ATSPM.sln && dotnet build ATSPM.sln && dotnet test ATSPM.sln"
echo "5) cd WebUI && npm ci && npm run build && npm test"
echo "6) docker compose up --build"

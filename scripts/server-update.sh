#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# BaGetter — Remote update script
#
# Run from your workstation to update the production server.
# Usage: bash scripts/server-update.sh [user@host]
#
# Default: root@164.90.168.212
###############################################################################

TARGET="${1:-root@164.90.168.212}"
DEPLOY_DIR="/opt/BaGetter"

echo "==> Updating BaGetter on ${TARGET}..."

ssh -o ConnectTimeout=10 "${TARGET}" bash -s "${DEPLOY_DIR}" <<'REMOTE'
set -euo pipefail
DEPLOY_DIR="$1"

cd "${DEPLOY_DIR}"

echo "── Pulling latest code..."
git pull --ff-only

echo "── Rebuilding and restarting BaGetter..."
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build bagetter

echo "── Waiting for health check..."
sleep 10
if docker compose -f docker-compose.yml -f docker-compose.prod.yml ps bagetter | grep -q healthy; then
  echo "── BaGetter is healthy."
else
  echo "── WARNING: BaGetter is not yet healthy. Check logs:"
  echo "   docker compose -f docker-compose.yml -f docker-compose.prod.yml logs bagetter --tail 30"
fi

echo "── Cleaning up build cache..."
docker builder prune -f
docker image prune -f

echo ""
echo "==> Update complete."
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
REMOTE

#!/usr/bin/env bash
# =============================================================================
# start.sh — Levantar el stack de desarrollo local via .NET Aspire
# Prerequisito: setup.sh ejecutado al menos una vez, Docker corriendo.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

export PATH="$HOME/.dotnet:$PATH"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║         Evidata — Stack de Desarrollo            ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

# ─── Verificar Docker ────────────────────────────────────────────────────────
docker info &>/dev/null || {
  echo -e "${RED}[ERROR]${NC} Docker no está corriendo. Inicia Docker Desktop y reintenta."
  exit 1
}

info "Iniciando stack via .NET Aspire..."
echo ""
echo -e "${CYAN}  URLs disponibles una vez iniciado:${NC}"
echo "  • Aspire Dashboard : http://localhost:18888"
echo "  • API REST         : http://localhost:5000"
echo "  • Swagger (dev)    : http://localhost:5000/openapi/v1.json"
echo "  • Health Check     : http://localhost:5000/health"
echo "  • Mailpit (emails) : http://localhost:8025"
echo "  • PostgreSQL       : localhost:5432  (DB: evidata_dev)"
echo "  • Azurite Blob     : http://127.0.0.1:10000/devstoreaccount1"
echo "  • Azurite Queue    : http://127.0.0.1:10001/devstoreaccount1"
echo ""
echo -e "${YELLOW}  Presiona Ctrl+C para detener el stack${NC}"
echo ""

cd "$REPO_ROOT/src/Evidata.AppHost"
dotnet run

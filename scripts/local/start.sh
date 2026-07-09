#!/usr/bin/env bash
# =============================================================================
# start.sh — Levantar el stack de desarrollo local via .NET Aspire
# Prerequisito: setup.sh ejecutado al menos una vez, Docker corriendo.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

export DOTNET_ROOT="$HOME/.dotnet"
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

info "Verificando instancias previas de Aspire..."
EXISTING_PID=$(ps aux | grep "Evidata\.AppHost$" | grep -v grep | awk '{print $2}' | head -1)
if [[ -n "$EXISTING_PID" ]]; then
  warn "Instancia previa detectada (PID $EXISTING_PID). Deteniendo..."
  kill "$EXISTING_PID" 2>/dev/null; sleep 3
  info "Instancia anterior detenida."
fi

# ─── Build con git hash para versionamiento ──────────────────────────────────
GIT_HASH=$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || echo "unknown")
BUILD_NUMBER=$(git -C "$REPO_ROOT" rev-list --count HEAD 2>/dev/null || echo "0")
info "Compilando (versión 1.0.0.${BUILD_NUMBER}+${GIT_HASH})..."
dotnet build "$REPO_ROOT/Evidata.sln" \
  -p:SourceRevisionId="$GIT_HASH" \
  -p:BuildNumber="$BUILD_NUMBER" \
  --nologo -v:q 2>&1 | grep -E "error|warning|Error|Warning" || true
info "Compilación completada."

info "Iniciando stack via .NET Aspire..."
echo ""
echo -e "${CYAN}  URLs disponibles una vez iniciado:${NC}"
echo "  • Aspire Dashboard : se muestra en la salida de Aspire"
echo "  • API REST         : puerto dinámico (ver dashboard)"
echo "  • Mailpit (emails) : http://localhost:8025"
echo ""
echo -e "${YELLOW}  Presiona Ctrl+C para detener el stack${NC}"
echo ""

cd "$REPO_ROOT/src/Evidata.AppHost"
dotnet run --no-build

#!/usr/bin/env bash
# =============================================================================
# setup.sh — Configuración inicial del ambiente de desarrollo local
# Ejecutar una vez al clonar el repositorio.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# ─── Colores ─────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error() { echo -e "${RED}[ERROR]${NC} $*"; exit 1; }

# ─── .NET en PATH ────────────────────────────────────────────────────────────
export PATH="$HOME/.dotnet:$PATH"

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║       Evidata — Setup de Ambiente Local          ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

# ─── Verificar pre-requisitos ────────────────────────────────────────────────
info "Verificando pre-requisitos..."

command -v dotnet &>/dev/null || error ".NET SDK no encontrado. Instala desde https://dot.net"
command -v docker  &>/dev/null || error "Docker no encontrado. Instala Docker Desktop."

DOTNET_VERSION=$(dotnet --version | cut -d. -f1)
if [[ "$DOTNET_VERSION" -lt 10 ]]; then
  error ".NET 10+ requerido. Versión actual: $(dotnet --version)"
fi

docker info &>/dev/null || error "Docker no está corriendo. Inicia Docker Desktop y reintenta."

info "Pre-requisitos OK"

# ─── Instalar workload de Aspire ─────────────────────────────────────────────
info "Instalando workload de .NET Aspire..."
dotnet workload install aspire 2>&1 | tail -3 || warn "El workload puede ya estar instalado."

# ─── Restaurar paquetes NuGet ────────────────────────────────────────────────
info "Restaurando paquetes NuGet..."
cd "$REPO_ROOT"
dotnet restore Evidata.sln --verbosity quiet

# ─── Configurar User Secrets — API ───────────────────────────────────────────
info "Configurando User Secrets para Evidata.Api..."
cd "$REPO_ROOT/src/Evidata.Api"

dotnet user-secrets set "ConnectionStrings:evidata-db" \
  "Host=localhost;Port=5432;Database=evidata_dev;Username=evidata;Password=evidata_local_pw"

dotnet user-secrets set "ConnectionStrings:evidata-storage" \
  "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OGLjX+N6+KymFfefef3fQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1"

dotnet user-secrets set "Email:Host"   "localhost"
dotnet user-secrets set "Email:Port"   "1025"
dotnet user-secrets set "Email:UseSsl" "false"

# ─── Configurar User Secrets — Worker Outbox ─────────────────────────────────
info "Configurando User Secrets para Evidata.Worker.Outbox..."
cd "$REPO_ROOT/src/Evidata.Worker.Outbox"

dotnet user-secrets set "ConnectionStrings:evidata-db" \
  "Host=localhost;Port=5432;Database=evidata_dev;Username=evidata;Password=evidata_local_pw" \
  2>/dev/null || warn "Worker.Outbox puede no tener UserSecretsId configurado, omitiendo."

# ─── Verificar build ─────────────────────────────────────────────────────────
info "Verificando que el proyecto compila..."
cd "$REPO_ROOT"
dotnet build Evidata.sln --configuration Debug --no-restore --verbosity quiet \
  && info "Build exitoso ✅" \
  || error "Build falló. Revisa los errores de compilación."

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║              Setup completado ✅                 ║"
echo "╠══════════════════════════════════════════════════╣"
echo "║  Próximos pasos:                                 ║"
echo "║  1. scripts/local/start.sh    → levantar stack  ║"
echo "║  2. scripts/local/migrate.sh  → aplicar DB      ║"
echo "║  3. scripts/local/seed.sh     → poblar datos    ║"
echo "║  4. scripts/local/smoke-test.sh → verificar     ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

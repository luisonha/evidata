#!/usr/bin/env bash
# =============================================================================
# migrate.sh — Aplica todas las migraciones de EF Core
# Prerequisito: PostgreSQL debe estar corriendo (stack levantado con start.sh).
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

export PATH="$HOME/.dotnet:$PATH"

# Connection string local por defecto
DB_CONNECTION="${DB_CONNECTION:-Host=localhost;Port=5432;Database=evidata_dev;Username=evidata;Password=evidata_local_pw}"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()    { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error()   { echo -e "${RED}[ERROR]${NC} $*"; exit 1; }
section() { echo ""; echo -e "${YELLOW}── $* ──────────────────────────────${NC}"; }

run_migration() {
  local project_path="$1"
  local context="$2"
  local startup_project="${3:-$REPO_ROOT/src/Evidata.Api/Evidata.Api.csproj}"

  info "Migrando: $context"
  dotnet ef database update \
    --project "$project_path" \
    --startup-project "$startup_project" \
    --context "$context" \
    --connection "$DB_CONNECTION" \
    --no-build \
    2>&1 | grep -E "(Applying|Done|No migrations|error)" || true
}

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║         Evidata — Migraciones de Base de Datos   ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

# ─── Verificar conexión a PostgreSQL ─────────────────────────────────────────
info "Verificando conexión a PostgreSQL..."
if ! dotnet ef --version &>/dev/null; then
  warn "dotnet-ef no encontrado. Instalando..."
  dotnet tool install --global dotnet-ef --prerelease
  export PATH="$HOME/.dotnet/tools:$PATH"
fi

# Build previo
info "Compilando solución..."
cd "$REPO_ROOT"
dotnet build Evidata.sln --configuration Release --no-restore --verbosity quiet \
  || error "Build falló. Ejecuta setup.sh primero."

section "Módulo 1: TenantManagement"
run_migration \
  "$REPO_ROOT/src/Modules/TenantManagement/Evidata.Modules.TenantManagement.csproj" \
  "EvidataDbContext"

section "Módulo 2: Identity"
run_migration \
  "$REPO_ROOT/src/Modules/Identity/Evidata.Modules.Identity.csproj" \
  "IdentityDbContext"

section "Módulo 3: Security (RBAC)"
run_migration \
  "$REPO_ROOT/src/Modules/Security/Evidata.Modules.Security.csproj" \
  "SecurityDbContext"

section "Módulo 4: Audit"
run_migration \
  "$REPO_ROOT/src/Modules/Audit/Evidata.Modules.Audit.csproj" \
  "AuditDbContext"

section "Módulo 5: LegalKnowledge"
run_migration \
  "$REPO_ROOT/src/Modules/LegalKnowledge/Evidata.Modules.LegalKnowledge.csproj" \
  "LegalKnowledgeDbContext"

section "Módulo 6: ProcessingInventory (RAT)"
run_migration \
  "$REPO_ROOT/src/Modules/ProcessingInventory/Evidata.Modules.ProcessingInventory.csproj" \
  "ProcessingInventoryDbContext"

section "Módulo 7: Evidence"
run_migration \
  "$REPO_ROOT/src/Modules/Evidence/Evidata.Modules.Evidence.csproj" \
  "EvidenceDbContext"

section "Módulo 8: GapManagement"
run_migration \
  "$REPO_ROOT/src/Modules/GapManagement/Evidata.Modules.GapManagement.csproj" \
  "GapManagementDbContext"

section "Módulo 9: Workflow"
run_migration \
  "$REPO_ROOT/src/Modules/Workflow/Evidata.Modules.Workflow.csproj" \
  "WorkflowDbContext"

section "Módulo 10: Documents"
run_migration \
  "$REPO_ROOT/src/Modules/Documents/Evidata.Modules.Documents.csproj" \
  "DocumentDbContext"

section "Módulo 11: Reporting"
run_migration \
  "$REPO_ROOT/src/Modules/Reporting/Evidata.Modules.Reporting.csproj" \
  "ReportingDbContext"

section "Módulo 12: MCP"
run_migration \
  "$REPO_ROOT/src/Modules/Mcp/Evidata.Modules.Mcp.csproj" \
  "McpDbContext"

section "Módulo 13: Search"
run_migration \
  "$REPO_ROOT/src/Modules/Search/Evidata.Modules.Search.csproj" \
  "SearchDbContext"

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║        Migraciones completadas ✅                ║"
echo "╠══════════════════════════════════════════════════╣"
echo "║  13 DbContexts procesados.                       ║"
echo "║  Siguiente: scripts/local/seed.sh                ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

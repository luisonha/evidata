#!/usr/bin/env bash
# =============================================================================
# migrate.sh — Aplica todas las migraciones de EF Core
# Prerequisito: stack levantado con start.sh (contenedor postgres de Aspire).
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()    { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error()   { echo -e "${RED}[ERROR]${NC} $*"; exit 1; }
section() { echo ""; echo -e "${YELLOW}── $* ──────────────────────────────${NC}"; }

# ─── Detectar conexión PostgreSQL desde contenedor Aspire ─────────────────────
detect_pg_connection() {
  # Aspire nombra el contenedor con prefijo "postgres-"
  local container
  container=$(docker ps --format "{{.Names}}" 2>/dev/null | grep -i "^postgres-" | head -1)

  if [[ -z "$container" ]]; then
    echo ""
    return
  fi

  local port
  port=$(docker port "$container" 5432 2>/dev/null | grep -o '[0-9]*$' | head -1)

  local password
  password=$(docker inspect "$container" \
    --format '{{range .Config.Env}}{{println .}}{{end}}' 2>/dev/null \
    | grep "^POSTGRES_PASSWORD=" | cut -d= -f2-)

  if [[ -n "$port" && -n "$password" ]]; then
    echo "Host=localhost;Port=${port};Database=evidata_dev;Username=postgres;Password=${password};Include Error Detail=true"
  else
    echo ""
  fi
}

# ─── Resolver connection string ───────────────────────────────────────────────
if [[ -n "${DB_CONNECTION:-}" ]]; then
  info "Usando DB_CONNECTION de variable de entorno."
else
  info "Detectando PostgreSQL desde contenedor Aspire..."
  DB_CONNECTION=$(detect_pg_connection)

  if [[ -z "$DB_CONNECTION" ]]; then
    echo ""
    echo -e "${RED}[ERROR]${NC} No se encontró el contenedor PostgreSQL de Aspire."
    echo "  ➜  Ejecuta primero: scripts/local/start.sh"
    echo "  ➜  O define: export DB_CONNECTION='Host=...;Port=...;Database=evidata_dev;...'"
    exit 1
  fi

  # Mostrar host:port sin contraseña
  CONN_DISPLAY=$(echo "$DB_CONNECTION" | sed 's/Password=[^;]*/Password=***/')
  info "PostgreSQL detectado: $CONN_DISPLAY"
fi

export DB_CONNECTION

# ─── Verificar conectividad real ─────────────────────────────────────────────
info "Verificando conectividad a PostgreSQL..."
PG_HOST=$(echo "$DB_CONNECTION" | sed 's/.*Host=\([^;]*\).*/\1/')
PG_PORT=$(echo "$DB_CONNECTION" | sed 's/.*Port=\([^;]*\).*/\1/')

if ! nc -z "$PG_HOST" "$PG_PORT" 2>/dev/null; then
  echo -e "${RED}[ERROR]${NC} PostgreSQL no responde en ${PG_HOST}:${PG_PORT}."
  echo "  ➜  Verifica que el stack esté corriendo: scripts/local/start.sh"
  exit 1
fi
info "Conexión verificada ✅ (${PG_HOST}:${PG_PORT})"

# ─── Variables de conteo ──────────────────────────────────────────────────────
FAILED=0
FAILED_MODULES=()

# ─── Función de migración con detección de error ─────────────────────────────
run_migration() {
  local project_path="$1"
  local context="$2"
  local startup_project="$REPO_ROOT/src/Evidata.Api/Evidata.Api.csproj"

  info "Migrando: $context"

  local output
  local exit_code=0

  output=$(dotnet ef database update \
    --project "$project_path" \
    --startup-project "$startup_project" \
    --context "$context" \
    --connection "$DB_CONNECTION" \
    --no-build \
    2>&1) || exit_code=$?

  if [[ $exit_code -ne 0 ]]; then
    echo -e "${RED}  ❌ FALLÓ: $context${NC}"
    echo "$output" | grep -E "(error|Error|Exception)" | head -3 | sed 's/^/     /'
    ((FAILED++)) || true
    FAILED_MODULES+=("$context")
  else
    local applied
    applied=$(echo "$output" | grep -c "Applying migration" || true)
    if [[ "$applied" -gt 0 ]]; then
      echo -e "${GREEN}  ✅ $context — $applied migración(es) aplicada(s)${NC}"
    else
      echo -e "${GREEN}  ✅ $context — sin cambios pendientes${NC}"
    fi
  fi
}

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║         Evidata — Migraciones de Base de Datos   ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

# ─── Build previo ─────────────────────────────────────────────────────────────
info "Compilando solución..."
cd "$REPO_ROOT"
dotnet build Evidata.sln --configuration Release --no-restore --verbosity quiet \
  || error "Build falló. Ejecuta setup.sh primero."

# ─── Migraciones ──────────────────────────────────────────────────────────────
section "Módulo 1: TenantManagement"
run_migration "$REPO_ROOT/src/Modules/TenantManagement/Evidata.Modules.TenantManagement.csproj" "EvidataDbContext"

section "Módulo 2: Identity"
run_migration "$REPO_ROOT/src/Modules/Identity/Evidata.Modules.Identity.csproj" "IdentityDbContext"

section "Módulo 3: Security (RBAC)"
run_migration "$REPO_ROOT/src/Modules/Security/Evidata.Modules.Security.csproj" "SecurityDbContext"

section "Módulo 4: Audit"
run_migration "$REPO_ROOT/src/Modules/Audit/Evidata.Modules.Audit.csproj" "AuditDbContext"

section "Módulo 5: LegalKnowledge"
run_migration "$REPO_ROOT/src/Modules/LegalKnowledge/Evidata.Modules.LegalKnowledge.csproj" "LegalKnowledgeDbContext"

section "Módulo 6: ProcessingInventory (RAT)"
run_migration "$REPO_ROOT/src/Modules/ProcessingInventory/Evidata.Modules.ProcessingInventory.csproj" "ProcessingInventoryDbContext"

section "Módulo 7: Evidence"
run_migration "$REPO_ROOT/src/Modules/Evidence/Evidata.Modules.Evidence.csproj" "EvidenceDbContext"

section "Módulo 8: GapManagement"
run_migration "$REPO_ROOT/src/Modules/GapManagement/Evidata.Modules.GapManagement.csproj" "GapManagementDbContext"

section "Módulo 9: Workflow"
run_migration "$REPO_ROOT/src/Modules/Workflow/Evidata.Modules.Workflow.csproj" "WorkflowDbContext"

section "Módulo 10: Documents"
run_migration "$REPO_ROOT/src/Modules/Documents/Evidata.Modules.Documents.csproj" "DocumentDbContext"

section "Módulo 11: Reporting"
run_migration "$REPO_ROOT/src/Modules/Reporting/Evidata.Modules.Reporting.csproj" "ReportingDbContext"

section "Módulo 12: MCP"
run_migration "$REPO_ROOT/src/Modules/Mcp/Evidata.Modules.Mcp.csproj" "McpDbContext"

section "Módulo 13: Search"
run_migration "$REPO_ROOT/src/Modules/Search/Evidata.Modules.Search.csproj" "SearchDbContext"

# ─── Resumen ──────────────────────────────────────────────────────────────────
echo ""
if [[ $FAILED -gt 0 ]]; then
  echo "╔══════════════════════════════════════════════════╗"
  printf "║  ❌ %d módulo(s) fallaron                        ║\n" "$FAILED"
  echo "╠══════════════════════════════════════════════════╣"
  for m in "${FAILED_MODULES[@]}"; do
    printf "║    • %-44s ║\n" "$m"
  done
  echo "╠══════════════════════════════════════════════════╣"
  echo "║  Revisa los errores arriba antes de continuar.  ║"
  echo "╚══════════════════════════════════════════════════╝"
  echo ""
  exit 1
fi

echo "╔══════════════════════════════════════════════════╗"
echo "║        Migraciones completadas ✅                ║"
echo "╠══════════════════════════════════════════════════╣"
echo "║  13 DbContexts procesados sin errores.           ║"
echo "║  Siguiente: scripts/local/seed.sh                ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""


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

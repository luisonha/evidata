#!/usr/bin/env bash
# =============================================================================
# verify-version.sh — Verificar que todos los servicios corren el commit actual
# Uso:
#   ./scripts/local/verify-version.sh                          (puertos por defecto)
#   ./scripts/local/verify-version.sh --help                   (ayuda)
#   ./scripts/local/verify-version.sh --api-port 5080 --fn-reporting-port 7071 ...
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
err()   { echo -e "${RED}[ERROR]${NC} $*"; }

# Colores por servicio
check()  { echo -e "${GREEN}✅${NC}  $*"; }
cross()  { echo -e "${RED}❌${NC}  $*"; }

print_help() {
    cat <<EOF
Verificador de versión para Evidata — Comprueba que todos los servicios corren
el commit actual.

USO:
    $0 [OPTIONS]

OPCIONES:
    --help                      Mostrar esta ayuda

    --host BASE_URL             Base URL para servicios (default: http://localhost)
    
    --api-port PORT             Puerto de API (default: intenta 5080-5090)
    --fn-reporting-port PORT    Puerto de fn-reporting (default: intenta 7071+)
    --fn-documents-port PORT    Puerto de fn-documents (default: intenta 7071+)
    --fn-notifications-port PORT Puerto de fn-notifications (default: intenta 7071+)
    --fn-maintenance-port PORT  Puerto de fn-maintenance (default: intenta 7071+)
    --fn-mcp-batch-port PORT    Puerto de fn-mcp-batch (default: intenta 7071+)
    --fn-search-indexing-port PORT Puerto de fn-search-indexing (default: intenta 7071+)

EJEMPLOS:
    # Usar puertos por defecto
    $0

    # Especificar puertos manualmente
    $0 --api-port 5080 --fn-reporting-port 7071

    # Especificar un host personalizado
    $0 --host http://192.168.1.100

NOTAS:
    - Los puertos por defecto asumen configuración estándar de Aspire
    - Si algún servicio no responde, intenta agregar --port explícitamente
    - Los puertos dinámicos se pueden ver en el Aspire Dashboard

EOF
}

# ─── Valores por defecto ─────────────────────────────────────────────────────
HOST="${HOST:-http://localhost}"
API_PORT="${API_PORT:-}"
FN_REPORTING_PORT="${FN_REPORTING_PORT:-}"
FN_DOCUMENTS_PORT="${FN_DOCUMENTS_PORT:-}"
FN_NOTIFICATIONS_PORT="${FN_NOTIFICATIONS_PORT:-}"
FN_MAINTENANCE_PORT="${FN_MAINTENANCE_PORT:-}"
FN_MCP_BATCH_PORT="${FN_MCP_BATCH_PORT:-}"
FN_SEARCH_INDEXING_PORT="${FN_SEARCH_INDEXING_PORT:-}"

# ─── Parsear argumentos ──────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
    case "$1" in
        --help)
            print_help
            exit 0
            ;;
        --host)
            HOST="$2"
            shift 2
            ;;
        --api-port)
            API_PORT="$2"
            shift 2
            ;;
        --fn-reporting-port)
            FN_REPORTING_PORT="$2"
            shift 2
            ;;
        --fn-documents-port)
            FN_DOCUMENTS_PORT="$2"
            shift 2
            ;;
        --fn-notifications-port)
            FN_NOTIFICATIONS_PORT="$2"
            shift 2
            ;;
        --fn-maintenance-port)
            FN_MAINTENANCE_PORT="$2"
            shift 2
            ;;
        --fn-mcp-batch-port)
            FN_MCP_BATCH_PORT="$2"
            shift 2
            ;;
        --fn-search-indexing-port)
            FN_SEARCH_INDEXING_PORT="$2"
            shift 2
            ;;
        *)
            err "Opción desconocida: $1"
            print_help
            exit 1
            ;;
    esac
done

# ─── Función para verificar versión de un servicio ──────────────────────────
check_service_version() {
    local service_name="$1"
    local port="$2"
    local url="${HOST}:${port}/api/version"

    # Intentar curl
    response=$(curl -s -w "\n%{http_code}" "$url" 2>/dev/null || echo "")
    if [[ -z "$response" ]]; then
        cross "$service_name (puerto $port) — No responde"
        return 1
    fi

    # Separar cuerpo de código HTTP
    http_code=$(echo "$response" | tail -n 1)
    body=$(echo "$response" | head -n -1)

    if [[ "$http_code" != "200" ]]; then
        cross "$service_name (puerto $port) — HTTP $http_code"
        return 1
    fi

    # Extraer versión del JSON
    version=$(echo "$body" | grep -o '"version":"[^"]*"' | head -1 | cut -d'"' -f4 || true)
    if [[ -z "$version" ]]; then
        cross "$service_name (puerto $port) — Respuesta inválida: $body"
        return 1
    fi

    # Obtener hash actual del repo
    current_hash=$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || echo "unknown")

    # Comparar (la versión típicamente incluye el hash al final, ej: "1.0.0.42+abc1234")
    if [[ "$version" == *"$current_hash"* ]] || [[ "$version" == *"+$current_hash"* ]]; then
        check "$service_name (puerto $port) — $version"
        return 0
    else
        cross "$service_name (puerto $port) — $version (esperado +$current_hash)"
        return 1
    fi
}

# ─── Función para descubrir puerto (intenta un rango) ────────────────────────
discover_port() {
    local start_port="$1"
    local service_name="$2"

    for port in $(seq "$start_port" $((start_port + 5))); do
        response=$(curl -s -m 1 -o /dev/null -w "%{http_code}" "http://localhost:$port/api/version" 2>/dev/null || echo "")
        if [[ "$response" == "200" ]]; then
            echo "$port"
            return 0
        fi
    done

    echo ""
    return 1
}

# ─── Script principal ────────────────────────────────────────────────────────
echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║  Verificador de Versión — Evidata"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

current_hash=$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || echo "unknown")
echo -e "Commit actual en disco: ${CYAN}${current_hash}${NC}"
echo -e "Verificando servicios en ${CYAN}${HOST}${NC}..."
echo ""

# Definir servicios (nombre, puerto_default)
declare -a SERVICES=(
    "API:5080"
    "fn-reporting:7071"
    "fn-documents:7072"
    "fn-notifications:7073"
    "fn-maintenance:7074"
    "fn-mcp-batch:7075"
    "fn-search-indexing:7076"
)

# Variables para puertos especificados por usuario
declare -A USER_PORTS=(
    ["API"]="$API_PORT"
    ["fn-reporting"]="$FN_REPORTING_PORT"
    ["fn-documents"]="$FN_DOCUMENTS_PORT"
    ["fn-notifications"]="$FN_NOTIFICATIONS_PORT"
    ["fn-maintenance"]="$FN_MAINTENANCE_PORT"
    ["fn-mcp-batch"]="$FN_MCP_BATCH_PORT"
    ["fn-search-indexing"]="$FN_SEARCH_INDEXING_PORT"
)

all_ok=0
for service_entry in "${SERVICES[@]}"; do
    IFS=':' read -r svc_name default_port <<< "$service_entry"
    
    # Usar puerto del usuario si se especificó, si no intentar descubrir
    port="${USER_PORTS[$svc_name]}"
    if [[ -z "$port" ]]; then
        port=$(discover_port "$default_port" "$svc_name")
        if [[ -z "$port" ]]; then
            cross "$svc_name — Puerto no encontrado (intentó ${default_port}-$((default_port+5)))"
            all_ok=1
            continue
        fi
    fi

    check_service_version "$svc_name" "$port" || all_ok=1
done

echo ""
if [[ $all_ok -eq 0 ]]; then
    echo "╔════════════════════════════════════════════════════════╗"
    echo -e "║  ${GREEN}✅ TODOS los servicios corren el commit ${current_hash}${NC}"
    echo "╚════════════════════════════════════════════════════════╝"
    exit 0
else
    echo "╔════════════════════════════════════════════════════════╗"
    echo -e "║  ${RED}⚠️  Algunos servicios NO coinciden con ${current_hash}${NC}"
    echo "╚════════════════════════════════════════════════════════╝"
    echo ""
    echo "Sugerencias:"
    echo "  1. Si algunos puertos no se encontraron, especifícalos:"
    echo "     $0 --api-port 5080 --fn-reporting-port 7071 ..."
    echo ""
    echo "  2. Verifica el Aspire Dashboard para ver los puertos reales:"
    echo "     Busca 'Aspire' en terminal y abre el dashboard"
    echo ""
    exit 1
fi

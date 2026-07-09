#!/usr/bin/env bash
# =============================================================================
# export-openapi.sh — Exportar el contrato OpenAPI desde la API en ejecución
#
# Uso:
#   bash scripts/local/export-openapi.sh              # detecta puerto dinámico
#   bash scripts/local/export-openapi.sh --url https://localhost:12345
#
# Salida: docs/openapi/openapi.json (commitable, actualizado)
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUTPUT="$REPO_ROOT/docs/openapi/openapi.json"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
ok()   { echo -e "${GREEN}✅${NC}  $*"; }
fail() { echo -e "${RED}❌${NC}  $*"; exit 1; }
info() { echo -e "${CYAN}ℹ${NC}   $*"; }

# ─── Resolver URL de la API ───────────────────────────────────────────────────
if [[ "${1:-}" == "--url" && -n "${2:-}" ]]; then
  API_URL="$2"
  info "Usando URL provista: $API_URL"
else
  info "Detectando puerto de la API..."
  API_URL=""
  for port in $(lsof -nP -i TCP -sTCP:LISTEN 2>/dev/null \
        | grep "^Evidata\." | awk '{print $9}' | grep -o '[0-9]*$' | sort -n | uniq); do
    code=$(curl -sk --insecure -o /dev/null -w "%{http_code}" --max-time 2 \
           "https://localhost:$port/health" 2>/dev/null || echo "000")
    if [[ "$code" == "200" || "$code" == "503" ]]; then
      API_URL="https://localhost:$port"
      break
    fi
  done
  [[ -z "$API_URL" ]] && fail "API no detectada. Levanta el stack con start.sh y reintenta."
  info "API detectada en $API_URL"
fi

# ─── Descargar openapi.json ───────────────────────────────────────────────────
OPENAPI_ENDPOINT="$API_URL/openapi/v1.json"
info "Descargando $OPENAPI_ENDPOINT ..."

HTTP_CODE=$(curl -sk --insecure -o /tmp/openapi-download.json \
  -w "%{http_code}" --max-time 10 "$OPENAPI_ENDPOINT" 2>/dev/null || echo "000")

[[ "$HTTP_CODE" != "200" ]] && fail "No se pudo descargar el OpenAPI (HTTP $HTTP_CODE). ¿La API está corriendo?"

# Validar que es JSON válido
python3 -c "import json,sys; json.load(open('/tmp/openapi-download.json'))" 2>/dev/null \
  || fail "La respuesta no es JSON válido."

# ─── Guardar y formatear ─────────────────────────────────────────────────────
mkdir -p "$(dirname "$OUTPUT")"
python3 -c "
import json, sys
with open('/tmp/openapi-download.json') as f:
    data = json.load(f)
with open('$OUTPUT', 'w') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)
    f.write('\n')
"

ok "OpenAPI exportado → $OUTPUT"

PATHS_COUNT=$(python3 -c "import json; d=json.load(open('$OUTPUT')); print(len(d.get('paths', {})))")
info "Paths documentados: $PATHS_COUNT"

echo ""
echo -e "${CYAN}Para commitear:${NC}"
echo "  git add docs/openapi/openapi.json"
echo "  git commit -m 'docs: actualizar openapi.json'"

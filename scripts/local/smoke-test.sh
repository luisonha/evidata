#!/usr/bin/env bash
# =============================================================================
# smoke-test.sh — Verifica que todos los módulos de la API responden correctamente
# Prerequisito: stack levantado (start.sh) + migraciones + seed aplicados
# =============================================================================
set -euo pipefail

TENANT_ID="${EVIDATA_TENANT:-00000000-0000-0000-0000-000000000001}"
ADMIN_ID="${EVIDATA_ADMIN:-00000000-0000-0000-0000-000000000010}"
USER_ID="${EVIDATA_USER:-00000000-0000-0000-0000-000000000011}"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'

PASS=0; FAIL=0; SKIP=0

pass() { echo -e "  ${GREEN}✅ PASS${NC}  $*"; (( PASS++ )) || true; }
fail() { echo -e "  ${RED}❌ FAIL${NC}  $*"; (( FAIL++ )) || true; }
skip() { echo -e "  ${YELLOW}⏭  SKIP${NC}  $*"; (( SKIP++ )) || true; }
detect_api_url() {
  [[ -n "${EVIDATA_API:-}" ]] && echo "$EVIDATA_API" && return

  # Buscar proceso Evidata.Api por nombre exacto en lsof (no el AppHost/Aspire)
  local ports
  ports=$(lsof -nP -i TCP -sTCP:LISTEN 2>/dev/null \
    | grep "^Evidata\." \
    | awk '{print $9}' | grep -o '[0-9]*$' | sort -n | uniq)

  if [[ -z "$ports" ]]; then
    local api_pid
    api_pid=$(ps aux | grep "Evidata\.Api$" | grep -v grep | awk '{print $2}' | head -1)
    if [[ -n "$api_pid" ]]; then
      ports=$(lsof -p "$api_pid" -nP -i TCP -sTCP:LISTEN 2>/dev/null \
        | awk '{print $9}' | grep -o '[0-9]*$' | sort -n | uniq)
    fi
  fi

  for port in $ports; do
    local code
    code=$(curl -sk -o /dev/null -w "%{http_code}" --max-time 3 \
      "https://localhost:${port}/health" 2>/dev/null || echo "000")
    if [[ "$code" == "200" || "$code" == "503" ]]; then
      echo "https://localhost:${port}"
      return
    fi
    code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 3 \
      "http://localhost:${port}/health" 2>/dev/null || echo "000")
    if [[ "$code" == "200" || "$code" == "503" ]]; then
      echo "http://localhost:${port}"
      return
    fi
  done
  echo ""
}

API=$(detect_api_url)

# ─── Helper: verificar endpoint ───────────────────────────────────────────────
check() {
  local label="$1"; local expected_min="$2"; local method="$3"
  local path="$4"; local body="${5:-}"

  local headers=(
    -H "X-Evidata-Dev-User: $ADMIN_ID"
    -H "X-Evidata-Dev-Tenant: $TENANT_ID"
    -H "X-Evidata-Dev-Email: admin@localdev.evidata"
  )

  local http_code
  if [[ -n "$body" ]]; then
    http_code=$(curl -sL --insecure -o /dev/null -w "%{http_code}" -X "$method" "$API$path" \
      "${headers[@]}" -H "Content-Type: application/json" -d "$body" 2>/dev/null || echo "000")
  else
    http_code=$(curl -sL --insecure -o /dev/null -w "%{http_code}" -X "$method" "$API$path" \
      "${headers[@]}" 2>/dev/null || echo "000")
  fi

  if [[ "$http_code" -ge "$expected_min" && "$http_code" -lt 400 ]]; then
    pass "$label (HTTP $http_code)"
  elif [[ "$http_code" == "000" ]]; then
    fail "$label — API no responde (conexión rechazada)"
  else
    fail "$label (HTTP $http_code esperado >=$expected_min)"
  fi
}

# ─── Helper: verificar que respuesta contiene valor ───────────────────────────
check_contains() {
  local label="$1"; local search="$2"; local path="$3"

  local headers=(
    -H "X-Evidata-Dev-User: $ADMIN_ID"
    -H "X-Evidata-Dev-Tenant: $TENANT_ID"
    -H "X-Evidata-Dev-Email: admin@localdev.evidata"
  )

  local body
  body=$(curl -sL --insecure "$API$path" "${headers[@]}" 2>/dev/null || echo "")

  if echo "$body" | grep -q "$search"; then
    pass "$label (contiene '$search')"
  else
    fail "$label (esperaba '$search' en respuesta: ${body:0:120})"
  fi
}

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║       Evidata — Smoke Tests (13 módulos)         ║"
echo "╚══════════════════════════════════════════════════╝"
echo -e "  API: ${CYAN}$API${NC}"
echo ""

# ─── Disponibilidad ──────────────────────────────────────────────────────────
echo -e "${CYAN}▶ Disponibilidad del servicio${NC}"

if [[ -z "$API" ]]; then
  echo -e "${RED}[FATAL]${NC} No se detectó la API de Evidata en ningún puerto."
  echo "   ➜  Ejecuta start.sh primero."
  exit 1
fi

HTTP_STATUS=$(curl -sL --insecure -o /dev/null -w "%{http_code}" "$API/health" 2>/dev/null || echo "000")

if [[ "$HTTP_STATUS" == "200" ]]; then
  pass "Health check — servicio saludable"
elif [[ "$HTTP_STATUS" == "503" ]]; then
  skip "Health check — servicio degradado (alguna dependencia no disponible)"
else
  fail "Health check — HTTP $HTTP_STATUS"
fi

check "GET /openapi/v1.json" 200 GET "/openapi/v1.json"

# ─── Módulo 1: TenantManagement ──────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ TenantManagement${NC}"

# POST idempotente: si el slug ya existe devuelve el tenant existente (200), si no lo crea (201)
TENANT_RESPONSE=$(curl -sL --insecure -X POST "$API/api/tenants" \
  -H "X-Evidata-Dev-User: $ADMIN_ID" \
  -H "X-Evidata-Dev-Tenant: $TENANT_ID" \
  -H "X-Evidata-Dev-Email: admin@localdev.evidata" \
  -H "Content-Type: application/json" \
  -d '{"slug": "empresa-demo", "name": "Empresa Demo S.A."}' 2>/dev/null || echo '{}')
REAL_TENANT_ID=$(echo "$TENANT_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4 || true)
if [[ -n "$REAL_TENANT_ID" ]]; then
  pass "POST /api/tenants (idempotente, id=$REAL_TENANT_ID)"
  # Nota: no sobreescribimos TENANT_ID — el seed insertó todos los datos bajo el ID fijo
  # $TENANT_ID = ${EVIDATA_TENANT:-00000000-0000-0000-0000-000000000001}
else
  fail "POST /api/tenants — no retornó id (respuesta: ${TENANT_RESPONSE:0:120})"
fi

check "GET /api/tenants/{id}" 200 GET "/api/tenants/$REAL_TENANT_ID"

# ─── Módulo 2: Identity ──────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Identity${NC}"

check "GET /api/users/{userId}" 200 GET "/api/users/$ADMIN_ID"

# ─── Módulo 3: Security / RBAC ───────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Security / RBAC${NC}"

check "GET /api/roles/users/{userId}" 200 GET \
  "/api/roles/users/$ADMIN_ID?tenantId=$TENANT_ID"
check "POST /api/roles/assign (idempotente)" 200 POST "/api/roles/assign" "{
  \"userId\": \"$ADMIN_ID\",
  \"roleId\": \"b0000001-0000-0000-0000-000000000001\",
  \"tenantId\": \"$TENANT_ID\"
}"

# ─── Módulo 4: Audit ─────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Audit${NC}"

check "GET /api/audit/tenant/{tenantId}" 200 GET "/api/audit/tenant/$TENANT_ID"

# ─── Módulo 5: LegalKnowledge ────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ LegalKnowledge${NC}"

check "GET /api/legal-sources" 200 GET "/api/legal-sources"
check "GET /api/legal-obligations" 200 GET "/api/legal-obligations"

# ─── Módulo 6: ProcessingInventory (RAT) ─────────────────────────────────────
echo ""
echo -e "${CYAN}▶ ProcessingInventory (RAT)${NC}"

check "GET /api/processing-activities" 200 GET "/api/processing-activities"
check_contains "RAT seed data existe" "Gesti" "/api/processing-activities"
check "GET /api/processing-activities (filtro aprobado)" 200 GET \
  "/api/processing-activities?status=Approved"

# ─── Módulo 7: Evidence ──────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Evidence${NC}"

check "GET /api/evidence" 200 GET "/api/evidence"

# ─── Módulo 8: GapManagement ─────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ GapManagement${NC}"

check "GET /api/gaps" 200 GET "/api/gaps"
check "GET /api/gaps (filtro crítico)" 200 GET "/api/gaps?severity=Critical"
check "GET /api/gaps/summary" 200 GET "/api/gaps/summary"

# ─── Módulo 9: Workflow ───────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Workflow${NC}"

check "GET /api/workflows" 200 GET "/api/workflows"
check "GET /api/workflows/pending" 200 GET "/api/workflows/pending"

# ─── Módulo 10: Documents ────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Documents${NC}"

check "GET /api/documents" 200 GET "/api/documents"
check "POST /api/documents/upload-url" 200 POST "/api/documents/upload-url" \
  '{"fileName":"smoke-test.pdf","contentType":"application/pdf","description":"smoke test"}'

# ─── Módulo 11: Reporting ────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Reporting${NC}"

check "GET /api/reports" 200 GET "/api/reports"
check "POST /api/reports (RAT)" 201 POST "/api/reports" \
  '{"reportType": "RAT", "parameters": "{}"}'
check "POST /api/reports (Gaps)" 201 POST "/api/reports" \
  '{"reportType": "Gaps", "parameters": "{}"}'

# ─── Módulo 12: MCP (asistente de cumplimiento) ───────────────────────────────
echo ""
echo -e "${CYAN}▶ MCP (asistente de cumplimiento)${NC}"

skip "POST /api/mcp/query — requiere configuración de LLM externo"
check "GET /api/mcp/interactions (historial)" 200 GET "/api/mcp/interactions"
check "GET /api/mcp/metrics/feedback" 200 GET "/api/mcp/metrics/feedback"
check "GET /api/mcp/metrics/hitl" 200 GET "/api/mcp/metrics/hitl"

# ─── Módulo 13: Search ───────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Search${NC}"

check "GET /api/search?q=datos" 200 GET "/api/search?q=datos"

# ─── Health Checks detallados ────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Health Checks detallados${NC}"

HEALTH_BODY=$(curl -sL --insecure "$API/health" 2>/dev/null || echo '{}')
if echo "$HEALTH_BODY" | grep -qi '"status".*"Healthy"'; then
  pass "Todos los servicios en estado Healthy"
elif echo "$HEALTH_BODY" | grep -qi "Degraded"; then
  skip "Algunos servicios degradados — revisar Aspire Dashboard: http://localhost:18888"
else
  skip "No se pudo determinar estado de health checks"
fi

# ─── Resumen ─────────────────────────────────────────────────────────────────
TOTAL=$((PASS + FAIL + SKIP))
echo ""
echo "╔══════════════════════════════════════════════════╗"
printf "║  Resultados: %2d PASS  %2d FAIL  %2d SKIP  de %2d  ║\n" $PASS $FAIL $SKIP $TOTAL
echo "╚══════════════════════════════════════════════════╝"
echo ""

if [[ $FAIL -gt 0 ]]; then
  echo -e "${YELLOW}ℹ  Algunos tests fallaron.${NC}"
  echo "   Causas comunes:"
  echo "   • Stack no completamente levantado — espera 30s y reintenta"
  echo "   • Migraciones no aplicadas — ejecuta migrate.sh"
  echo "   • Seed no ejecutado — ejecuta seed.sh"
  echo "   • Revisar logs: http://localhost:18888 (Aspire Dashboard)"
  echo ""
  exit 1
fi

echo -e "${GREEN}✅ Todos los tests pasaron. El stack está listo para desarrollo.${NC}"
echo ""

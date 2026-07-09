#!/usr/bin/env bash
# =============================================================================
# smoke-test.sh — Verifica que todos los módulos de la API responden correctamente
# Prerequisito: stack levantado (start.sh) + migraciones + seed aplicados
# =============================================================================
set -euo pipefail

API="${EVIDATA_API:-http://localhost:5000}"
TENANT_ID="${EVIDATA_TENANT:-00000000-0000-0000-0000-000000000001}"
ADMIN_ID="${EVIDATA_ADMIN:-00000000-0000-0000-0000-000000000010}"
USER_ID="${EVIDATA_USER:-00000000-0000-0000-0000-000000000011}"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'

PASS=0; FAIL=0; SKIP=0

pass() { echo -e "  ${GREEN}✅ PASS${NC}  $*"; ((PASS++)); }
fail() { echo -e "  ${RED}❌ FAIL${NC}  $*"; ((FAIL++)); }
skip() { echo -e "  ${YELLOW}⏭  SKIP${NC}  $*"; ((SKIP++)); }

# ─── Helper: verificar endpoint ───────────────────────────────────────────────
check() {
  local label="$1"; local expected_min="$2"; local method="$3"
  local path="$4"; local body="${5:-}"

  local headers=(
    -H "X-Evidata-Dev-User: $ADMIN_ID"
    -H "X-Evidata-Dev-Tenant: $TENANT_ID"
    -H "X-Evidata-Dev-Email: admin@localdev.evidata"
  )

  local response http_code
  if [[ -n "$body" ]]; then
    http_code=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$API$path" \
      "${headers[@]}" -H "Content-Type: application/json" -d "$body" 2>/dev/null || echo "000")
  else
    http_code=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$API$path" \
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
  body=$(curl -s "$API$path" "${headers[@]}" 2>/dev/null || echo "")

  if echo "$body" | grep -q "$search"; then
    pass "$label (contiene '$search')"
  else
    fail "$label (esperaba '$search' en respuesta: ${body:0:100})"
  fi
}

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║       Evidata — Smoke Tests                      ║"
echo "╚══════════════════════════════════════════════════╝"
echo -e "  API: ${CYAN}$API${NC}"
echo ""

# ─── Verificar disponibilidad ────────────────────────────────────────────────
echo -e "${CYAN}▶ Disponibilidad del servicio${NC}"

HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API/health" 2>/dev/null || echo "000")
if [[ "$HTTP_STATUS" == "000" ]]; then
  echo -e "${RED}[FATAL]${NC} API no disponible en $API. Ejecuta start.sh primero."
  exit 1
fi

if [[ "$HTTP_STATUS" == "200" ]]; then
  pass "Health check — servicio saludable"
elif [[ "$HTTP_STATUS" == "503" ]]; then
  skip "Health check — servicio degradado (alguna dependencia no disponible)"
else
  fail "Health check — HTTP $HTTP_STATUS"
fi

# ─── Módulo: TenantManagement ────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ TenantManagement${NC}"

check "GET /api/tenants/{id}" 200 GET "/api/tenants/$TENANT_ID"
check "POST /api/tenants (idempotente)" 200 POST "/api/tenants" \
  '{"slug": "smoke-test-tenant", "name": "Smoke Test Tenant"}'

# ─── Módulo: Identity ────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Identity${NC}"

check "GET /api/users/profile" 200 GET "/api/users/profile"

# ─── Módulo: Security / RBAC ─────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Security / RBAC${NC}"

check "GET /api/roles/users/{userId}" 200 GET \
  "/api/roles/users/$ADMIN_ID?tenantId=$TENANT_ID"

check "POST /api/roles/assign (idempotente)" 200 POST "/api/roles/assign" "{
  \"userId\": \"$ADMIN_ID\",
  \"roleId\": \"b0000001-0000-0000-0000-000000000001\",
  \"tenantId\": \"$TENANT_ID\"
}"

# ─── Módulo: Audit ───────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Audit${NC}"

check "GET /api/audit" 200 GET "/api/audit?tenantId=$TENANT_ID"

# ─── Health detail ───────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ Health Checks detallados${NC}"

check "GET /health" 200 GET "/health"

HEALTH_BODY=$(curl -s "$API/health" 2>/dev/null || echo '{}')
if echo "$HEALTH_BODY" | grep -qi '"status".*"Healthy"'; then
  pass "Todos los health checks en estado Healthy"
elif echo "$HEALTH_BODY" | grep -qi "Degraded"; then
  skip "Algunos servicios degradados — revisar Aspire Dashboard en http://localhost:18888"
fi

# ─── OpenAPI ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}▶ OpenAPI Schema${NC}"

check "GET /openapi/v1.json" 200 GET "/openapi/v1.json"

# ─── Resumen ──────────────────────────────────────────────────────────────────
TOTAL=$((PASS + FAIL + SKIP))
echo ""
echo "╔══════════════════════════════════════════════════╗"
printf "║  Resultados: %2d PASS  %2d FAIL  %2d SKIP  de %2d  ║\n" $PASS $FAIL $SKIP $TOTAL
echo "╚══════════════════════════════════════════════════╝"
echo ""

if [[ $FAIL -gt 0 ]]; then
  echo -e "${YELLOW}ℹ  Algunos tests fallaron.${NC}"
  echo "   Posibles causas:"
  echo "   • El stack no está completamente levantado (espera 30s y reintenta)"
  echo "   • Las migraciones no se han aplicado (ejecuta migrate.sh)"
  echo "   • El seed no se ha ejecutado (ejecuta seed.sh)"
  echo "   • Revisar logs en Aspire Dashboard: http://localhost:18888"
  echo ""
  exit 1
fi

if [[ $FAIL -eq 0 ]]; then
  echo -e "${GREEN}✅ Todos los tests pasaron. El stack está listo para desarrollo.${NC}"
  echo ""
fi

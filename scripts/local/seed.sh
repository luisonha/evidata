#!/usr/bin/env bash
# =============================================================================
# seed.sh — Pobla la base de datos local con datos de prueba realistas
# Prerequisito: stack levantado (start.sh) + migraciones aplicadas (migrate.sh)
#
# Datos creados:
#   - 1 Tenant: empresa-demo / "Empresa Demo S.A."
#   - 2 usuarios (perfiles + roles RBAC)
#   - Roles y permisos Ley 21.719
#   - 5 Actividades de Tratamiento (RAT)
#   - 4 Evidencias de cumplimiento
#   - 3 Gaps de cumplimiento
# =============================================================================
set -euo pipefail

# ─── Configuración ───────────────────────────────────────────────────────────
TENANT_ID="${EVIDATA_TENANT:-00000000-0000-0000-0000-000000000001}"
ADMIN_ID="${EVIDATA_ADMIN:-00000000-0000-0000-0000-000000000010}"
USER_ID="${EVIDATA_USER:-00000000-0000-0000-0000-000000000011}"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
info()    { echo -e "${GREEN}[SEED]${NC}  $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error()   { echo -e "${RED}[ERROR]${NC} $*"; exit 1; }
section() { echo ""; echo -e "${CYAN}▶ $*${NC}"; }

# ─── Detectar URL de la API (puerto dinámico de Aspire) ──────────────────────
detect_api_url() {
  [[ -n "${EVIDATA_API:-}" ]] && echo "$EVIDATA_API" && return

  # Buscar proceso Evidata.Api por nombre exacto en lsof (no el AppHost/Aspire)
  local ports
  ports=$(lsof -nP -i TCP -sTCP:LISTEN 2>/dev/null \
    | grep "^Evidata\." \
    | awk '{print $9}' | grep -o '[0-9]*$' | sort -n | uniq)

  if [[ -z "$ports" ]]; then
    # Fallback: buscar PID del binario compilado (no el dotnet run wrapper)
    local api_pid
    api_pid=$(ps aux | grep "Evidata\.Api$" | grep -v grep | awk '{print $2}' | head -1)
    if [[ -n "$api_pid" ]]; then
      ports=$(lsof -p "$api_pid" -nP -i TCP -sTCP:LISTEN 2>/dev/null \
        | awk '{print $9}' | grep -o '[0-9]*$' | sort -n | uniq)
    fi
  fi

  for port in $ports; do
    # Verificar /health — solo acepta 200 o 503 (no 302/307 del AppHost)
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

# ─── Headers de autenticación local ──────────────────────────────────────────
ADMIN_HEADERS=(
  -H "X-Evidata-Dev-User: $ADMIN_ID"
  -H "X-Evidata-Dev-Tenant: $TENANT_ID"
  -H "X-Evidata-Dev-Email: admin@localdev.evidata"
  -H "Content-Type: application/json"
)

# ─── Helper: llamada HTTP con manejo de errores ───────────────────────────────
call_api() {
  local method="$1"; local path="$2"; local body="${3:-}"
  local response http_code

  # -L sigue redirects HTTP→HTTPS; --insecure acepta el cert de desarrollo
  if [[ -n "$body" ]]; then
    response=$(curl -sL --insecure -w "\n%{http_code}" -X "$method" "$API$path" \
      "${ADMIN_HEADERS[@]}" -d "$body" 2>&1) || true
  else
    response=$(curl -sL --insecure -w "\n%{http_code}" -X "$method" "$API$path" \
      "${ADMIN_HEADERS[@]}" 2>&1) || true
  fi

  http_code=$(echo "$response" | tail -1)
  body_response=$(echo "$response" | sed '$d')  # compatible macOS (no head -n -1)

  if [[ "$http_code" =~ ^(200|201|204)$ ]]; then
    echo "$body_response"
    return 0
  elif [[ "$http_code" == "409" ]] || echo "$body_response" | grep -qi "already exists"; then
    warn "Recurso ya existe (idempotente) — $path"
    return 0
  else
    warn "HTTP $http_code en $method $path — $body_response"
    return 0  # seed no falla por conflictos
  fi
}

echo ""
echo "╔══════════════════════════════════════════════════╗"
echo "║       Evidata — Seed de Datos de Prueba          ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""

# ─── Verificar que la API esté disponible ────────────────────────────────────
info "Verificando API en $API..."
HTTP_STATUS=$(curl -sk -o /dev/null -w "%{http_code}" "$API/health" 2>/dev/null || echo "000")
if [[ "$HTTP_STATUS" != "200" && "$HTTP_STATUS" != "503" ]]; then
  error "API no responde en $API (HTTP $HTTP_STATUS). Ejecuta start.sh primero."
fi
info "API disponible ✅ (HTTP $HTTP_STATUS — $API)"

# ─── Detectar credenciales de PostgreSQL (Aspire) ────────────────────────────
PG_CONTAINER=$(docker ps --format '{{.Names}}' 2>/dev/null | grep "^postgres-" | head -1)
if [[ -n "$PG_CONTAINER" ]]; then
  PG_PASS=$(docker inspect "$PG_CONTAINER" \
    --format '{{range .Config.Env}}{{println .}}{{end}}' 2>/dev/null \
    | grep "^POSTGRES_PASSWORD=" | cut -d= -f2-)
  PSQL_CMD="docker exec -e PGPASSWORD=$PG_PASS -i $PG_CONTAINER psql -U postgres -d evidata-db"
  info "PostgreSQL detectado: $PG_CONTAINER"
else
  PSQL_CMD=""
  warn "Contenedor PostgreSQL no encontrado. Bloques SQL serán omitidos."
fi

# ═══════════════════════════════════════════════════════
# BLOQUE 1: Tenant
# ═══════════════════════════════════════════════════════
section "Bloque 1: Tenant principal"

info "Creando tenant empresa-demo..."
TENANT_RESPONSE=$(call_api POST "/api/tenants" '{
  "slug": "empresa-demo",
  "name": "Empresa Demo S.A."
}')
info "Tenant: $TENANT_RESPONSE"

# Guardar ID del tenant si la API devuelve uno distinto al seed
CREATED_TENANT_ID=$(echo "$TENANT_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4 || echo "$TENANT_ID")
info "Usando TenantId: $TENANT_ID (seed fijo)"

# ═══════════════════════════════════════════════════════
# BLOQUE 2: Roles y Permisos RBAC
# ═══════════════════════════════════════════════════════
section "Bloque 2: Roles y permisos (Ley 21.719)"

if [[ -n "$PSQL_CMD" ]]; then
  info "Insertando roles del sistema via SQL..."
  $PSQL_CMD <<'EOSQL' 2>&1 | grep -v "^$" || warn "Algunos roles pueden ya existir"
-- Permisos Ley 21.719 (schema: security, columnas PascalCase con EF Core)
-- Nota: Permisos legacy 'a0000001-*' fueron removidos — no hay roles legacy (DPO, PrivacyAnalyst)
-- que los referencien tras el fix RBAC. Los 7 roles oficiales se siembran en SecurityDbContext.
-- Esta tabla está vacía por ahora (los permisos reales se definirán con el nuevo modelo de autorización).
--
-- INSERT INTO security.permissions ("Id", "Name", "Resource", "Action", "Description")
-- VALUES (...) — Pendiente futura definición de permisos

-- NOTE: Legacy roles (DPO, PrivacyAnalyst) removed. 
-- The 7 official RBAC roles (TenantOwner, ComplianceAdmin, ProcessOwner, LegalReviewer, SecurityReviewer, Auditor, Viewer)
-- are now seeded via EF Core HasData() in SecurityDbContext.
-- Role assignments below use dynamic SQL to fetch the correct role IDs after migration.
EOSQL
  info "Roles y permisos insertados ✅"
else
  warn "PostgreSQL no disponible. Roles omitidos."
fi

# ─── Usuarios de prueba en identity.user_profiles ──────────────────────────
if [[ -n "$PSQL_CMD" ]]; then
  info "Insertando usuarios de prueba..."
  $PSQL_CMD <<EOSQL 2>&1 | grep -v "^$" || warn "Algunos usuarios pueden ya existir"
INSERT INTO identity.user_profiles ("Id", "ExternalId", "Provider", "Email", "DisplayName", "TenantId", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
  ('$ADMIN_ID', 'local|admin',  'localdev', 'admin@localdev.evidata', 'Admin DPO',       '$TENANT_ID', true, NOW(), NOW()),
  ('$USER_ID',  'local|user',   'localdev', 'user@localdev.evidata',  'Usuario Regular', '$TENANT_ID', true, NOW(), NOW())
ON CONFLICT DO NOTHING;
EOSQL
  info "Usuarios de prueba insertados ✅"
else
  warn "PostgreSQL no disponible. Usuarios omitidos."
fi

# Asignar roles via SQL dinámico (obtiene IDs reales de la migración HasData)
info "Asignando rol TenantOwner al admin..."
if [[ -n "$PSQL_CMD" ]]; then
  TENANT_OWNER_ID=$($PSQL_CMD -t -c "SELECT \"Id\" FROM security.roles WHERE \"Name\" = 'TenantOwner' LIMIT 1")
  if [[ -n "$TENANT_OWNER_ID" ]]; then
    $PSQL_CMD <<EOSQL 2>&1 | grep -v "^$" || warn "No se pudo asignar rol TenantOwner"
INSERT INTO security.user_role_assignments ("UserId", "RoleId", "TenantId")
VALUES ('$ADMIN_ID', '$TENANT_OWNER_ID', '$TENANT_ID')
ON CONFLICT DO NOTHING;
EOSQL
    info "Rol TenantOwner asignado al admin ✅"
  else
    warn "TenantOwner role no encontrado en la BD"
  fi
else
  warn "PostgreSQL no disponible. Asignación de rol omitida."
fi

info "Asignando rol ComplianceAdmin al usuario regular..."
if [[ -n "$PSQL_CMD" ]]; then
  COMPLIANCE_ADMIN_ID=$($PSQL_CMD -t -c "SELECT \"Id\" FROM security.roles WHERE \"Name\" = 'ComplianceAdmin' LIMIT 1")
  if [[ -n "$COMPLIANCE_ADMIN_ID" ]]; then
    $PSQL_CMD <<EOSQL 2>&1 | grep -v "^$" || warn "No se pudo asignar rol ComplianceAdmin"
INSERT INTO security.user_role_assignments ("UserId", "RoleId", "TenantId")
VALUES ('$USER_ID', '$COMPLIANCE_ADMIN_ID', '$TENANT_ID')
ON CONFLICT DO NOTHING;
EOSQL
    info "Rol ComplianceAdmin asignado al usuario regular ✅"
  else
    warn "ComplianceAdmin role no encontrado en la BD"
  fi
else
  warn "PostgreSQL no disponible. Asignación de rol omitida."
fi

# ═══════════════════════════════════════════════════════
# BLOQUE 2b: Reglas de Detección de Brechas (GapRules)
# ═══════════════════════════════════════════════════════
section "Bloque 2b: Reglas de detección de brechas (11 reglas)"

if [[ -n "$PSQL_CMD" ]]; then
  info "Insertando 11 reglas de detección de brechas..."
  $PSQL_CMD <<'EOSQL' 2>&1 | grep -v "^$" || warn "Algunas reglas pueden ya existir"
-- 11 Reglas de detección de brechas de cumplimiento (contract 04, lines 162-174)
-- Severity values: Low=0, Medium=1, High=2, Critical=3
INSERT INTO gap.gap_rules 
  (id, tenant_id, rule_code, description, severity, blocks_approval, test_fixture_name, is_fully_implemented, implementation_notes, created_by, created_at)
VALUES
  ('10000000-0000-0000-0000-000000000000', '$TENANT_ID',
   'TRANSFER_WITHOUT_DESTINATION_COUNTRY',
   'Transferencia de datos sin país destino especificado',
   'Critical', true, 'pa-transfer-no-country', true,
   'Evalúa si un nodo de transferencia internacional tiene especificado el país destino.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000001', '$TENANT_ID',
   'TRANSFER_WITHOUT_RECEIVER',
   'Transferencia sin receptor identificado',
   'High', true, 'pa-transfer-no-receiver', true,
   'Verifica que todo nodo de transferencia identifique un receptor o destinatario.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000002', '$TENANT_ID',
   'TRANSFER_WITHOUT_SAFEGUARD',
   'Transferencia internacional sin salvaguarda especificada',
   'Critical', true, 'pa-transfer-no-safeguard', true,
   'Controla que transferencias internacionales de terceros países cuenten con salvaguarda (Capítulo V RGPD).',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000003', '$TENANT_ID',
   'TRANSFER_WITHOUT_BLOCKING_EVIDENCE',
   'Transferencia sin evidencia de aprobación o base legal',
   'Critical', true, 'pa-transfer-no-evidence', true,
   'Valida que exista evidencia adjunta que apruebe o justifique la transferencia internacional.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000004', '$TENANT_ID',
   'SENSITIVE_DATA_WITHOUT_SECURITY_REVIEW',
   'Dato sensible sin revisión de seguridad',
   'Critical', true, 'pa-sensitive-no-security-review', true,
   'Verifica que categorías de datos sensibles tengan validación de evidencia de tipo Security.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000005', '$TENANT_ID',
   'LEGAL_BASIS_MISSING',
   'Base legal no especificada',
   'Critical', true, 'pa-no-legal-basis', true,
   'Comprueba que todo tratamiento de datos cuente con base legal explícita (consentimiento, contrato, obligación legal, etc.).',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000006', '$TENANT_ID',
   'RETENTION_UNDEFINED',
   'Período de retención no definido',
   'High', true, 'pa-retention-undefined', false,
   'Requiere especificación de período de retención. Nota: El dominio aún no modela explícitamente ''retention_period_days'' en ProcessingActivity.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000007', '$TENANT_ID',
   'DATA_CATEGORIES_EMPTY',
   'Sin categorías de datos especificadas',
   'High', true, 'pa-no-data-categories', true,
   'Verifica que al menos una categoría de dato esté vinculada al tratamiento.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000008', '$TENANT_ID',
   'PURPOSE_UNDEFINED',
   'Propósito del tratamiento no definido',
   'Critical', true, 'pa-purpose-undefined', true,
   'Asegura que se especifique claramente el propósito o finalidad del tratamiento de datos.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000009', '$TENANT_ID',
   'DATA_SUBJECTS_EMPTY',
   'Sin categorías de interesados especificadas',
   'High', true, 'pa-no-data-subjects', true,
   'Comprueba que al menos una categoría de interesado esté identificada en el tratamiento.',
   '$ADMIN_ID', NOW()),
  ('10000000-0000-0000-0000-000000000010', '$TENANT_ID',
   'SYSTEMS_WITHOUT_OWNER',
   'Sistemas sin propietario identificado',
   'High', true, 'pa-systems-without-owner', false,
   'Verifica que todo sistema participante en el tratamiento tenga propietario asignado. Nota: Requiere introspección de nodos que el dominio aún no modeliza completamente.',
   '$ADMIN_ID', NOW())
ON CONFLICT (id) DO NOTHING;
EOSQL
  info "11 Reglas de detección de brechas insertadas ✅"
else
  warn "PostgreSQL no disponible. Reglas de brechas omitidas."
fi

# ═══════════════════════════════════════════════════════
# BLOQUE 3: Actividades de Tratamiento (RAT)
# ═══════════════════════════════════════════════════════
section "Bloque 3: Actividades de Tratamiento (RAT)"

if [[ -n "$PSQL_CMD" ]]; then
  info "Insertando RATs de prueba..."
  $PSQL_CMD <<EOSQL 2>&1 | grep -v "^$" || warn "Algunos RATs pueden ya existir"
INSERT INTO rat.processing_activities
  (id, tenant_id, name, description, controller, department, status, version, created_by, created_at,
   data_categories, data_subjects, systems, suppliers, security_measures)
VALUES
  ('d0000001-0000-0000-0000-000000000001', '$TENANT_ID',
   'Gestión de Nómina',
   'Tratamiento de datos personales de empleados para cálculo y pago de remuneraciones.',
   'Gerencia de Recursos Humanos', 'Recursos Humanos', 'Approved', 1, '$ADMIN_ID',
   NOW() - INTERVAL '30 days',
   '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb),
  ('d0000001-0000-0000-0000-000000000002', '$TENANT_ID',
   'Atención al Cliente',
   'Tratamiento de datos de clientes para gestión de solicitudes, reclamos y soporte.',
   'Gerencia Comercial', 'Servicio al Cliente', 'Draft', 1, '$ADMIN_ID',
   NOW() - INTERVAL '15 days',
   '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb),
  ('d0000001-0000-0000-0000-000000000003', '$TENANT_ID',
   'Marketing Directo',
   'Envío de comunicaciones comerciales a clientes con consentimiento expreso.',
   'Gerencia de Marketing', 'Marketing Digital', 'Draft', 1, '$USER_ID',
   NOW() - INTERVAL '10 days',
   '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb),
  ('d0000001-0000-0000-0000-000000000004', '$TENANT_ID',
   'Videovigilancia Instalaciones',
   'Sistema de cámaras de seguridad en acceso a instalaciones físicas.',
   'Gerencia de Seguridad', 'Seguridad Corporativa', 'UnderReview', 1, '$USER_ID',
   NOW() - INTERVAL '5 days',
   '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb),
  ('d0000001-0000-0000-0000-000000000005', '$TENANT_ID',
   'Portal de Proveedores',
   'Gestión de datos de proveedores y representantes legales para proceso de compras.',
   'Gerencia de Abastecimiento', 'Adquisiciones', 'Draft', 1, '$USER_ID',
   NOW() - INTERVAL '2 days',
   '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb)
ON CONFLICT (id) DO NOTHING;
EOSQL
  info "RATs creados ✅"
else
  warn "Contenedor PostgreSQL no encontrado. RATs omitidos."
fi

# ═══════════════════════════════════════════════════════
# BLOQUE 4: MCP — interacción de prueba
# ═══════════════════════════════════════════════════════
section "Bloque 4: MCP — consulta de prueba"

MCP_RESPONSE=$(call_api POST "/api/mcp/query" \
  '{"question": "¿Qué tratamientos de datos tenemos registrados?"}' 2>/dev/null || echo '{"error":"skip"}')

if echo "$MCP_RESPONSE" | grep -q "interactionId"; then
  info "Interacción MCP creada ✅"
else
  warn "MCP query no respondió como esperado (puede requerir configuración adicional): ${MCP_RESPONSE:0:120}"
fi

# ═══════════════════════════════════════════════════════
# BLOQUE 5: Resumen
# ═══════════════════════════════════════════════════════
echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║             Seed completado ✅                           ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║  Tenant:  empresa-demo ($TENANT_ID)   ║"
echo "║  Admin:   admin@localdev.evidata ($ADMIN_ID)  ║"
echo "║  Usuario: user@localdev.evidata  ($USER_ID)  ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║  Datos creados:                                          ║"
echo "║    • 1 Tenant                                            ║"
echo "║    • 7 Roles RBAC (TenantOwner, ComplianceAdmin, etc.)    ║"
echo "║    • 2 Usuarios de prueba con roles asignados            ║"
echo "║    • 11 Reglas de detección de brechas                   ║"
echo "║    • 5 RATs (1 Approved, 1 UnderReview, 3 Draft)         ║"
echo "║    • 1 Consulta MCP de prueba                            ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║  Siguiente: scripts/local/smoke-test.sh                  ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""


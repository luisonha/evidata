#!/usr/bin/env bash
# =============================================================================
# reset.sh — Resetea la base de datos y vuelve a hacer seed
# ⚠️  DESTRUCTIVO: elimina todos los datos de evidata_dev
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

RED='\033[0;31m'; YELLOW='\033[1;33m'; GREEN='\033[0;32m'; NC='\033[0m'

echo ""
echo -e "${RED}╔══════════════════════════════════════════════════╗${NC}"
echo -e "${RED}║  ⚠️  RESET DESTRUCTIVO DE BASE DE DATOS          ║${NC}"
echo -e "${RED}║  Se eliminarán TODOS los datos de evidata_dev    ║${NC}"
echo -e "${RED}╚══════════════════════════════════════════════════╝${NC}"
echo ""

read -r -p "¿Confirmas el reset? (escribe 'RESET' para continuar): " CONFIRM
if [[ "$CONFIRM" != "RESET" ]]; then
  echo "Cancelado."
  exit 0
fi

echo ""
echo -e "${YELLOW}[RESET]${NC} Dropping y recreando la base de datos..."

CONTAINER=$(docker ps --format '{{.Names}}' 2>/dev/null | grep -i postgres | head -1)
if [[ -z "$CONTAINER" ]]; then
  echo -e "${RED}[ERROR]${NC} Contenedor PostgreSQL no encontrado. ¿Está el stack levantado?"
  exit 1
fi

# Drop y recrear
docker exec "$CONTAINER" psql -U evidata -c "DROP DATABASE IF EXISTS evidata_dev;" postgres 2>&1 || true
docker exec "$CONTAINER" psql -U evidata -c "CREATE DATABASE evidata_dev;" postgres

echo -e "${GREEN}[RESET]${NC} Base de datos recreada. Aplicando migraciones..."
bash "$SCRIPT_DIR/migrate.sh"

echo -e "${GREEN}[RESET]${NC} Poblando datos de prueba..."
bash "$SCRIPT_DIR/seed.sh"

echo ""
echo -e "${GREEN}✅ Reset completado. La base de datos está limpia con datos seed.${NC}"
echo ""

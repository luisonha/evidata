#!/usr/bin/env bash
# =============================================================================
# configure-branch-protection.sh
# Configura branch protection para main y develop con el app_id correcto
# de GitHub Actions (15368), evitando el bloqueo
# "CI / build-and-test Expected — Waiting for status to be reported".
#
# Causa del problema: la UI de GitHub y la API legacy registran el check
# como "CI / build-and-test" con app_id=null (commit status). GitHub Actions
# reporta el check como "build-and-test" con app_id=15368 (check run).
# Son contextos distintos — el legacy nunca llega, siempre queda "waiting".
#
# Ejecutar: bash scripts/repo/configure-branch-protection.sh
# Requisito: gh CLI autenticado con permisos de admin en el repo
# =============================================================================
set -euo pipefail

REPO="luisonha/evidata"
GHA_APP_ID=15368   # GitHub Actions app_id — constante en todos los repos
CHECK_NAME="build-and-test"  # nombre real del job en .github/workflows/ci.yml

GREEN='\033[0;32m'; CYAN='\033[0;36m'; NC='\033[0m'

echo -e "\n${CYAN}Configurando branch protection — $REPO${NC}\n"

# ─── develop ─────────────────────────────────────────────────────────────────
echo "▶ develop..."
gh api --method PUT "repos/$REPO/branches/develop/protection" \
  --field "required_status_checks[strict]=false" \
  --field "required_status_checks[checks][][context]=$CHECK_NAME" \
  --field "required_status_checks[checks][][app_id]=$GHA_APP_ID" \
  --field "required_pull_request_reviews[required_approving_review_count]=1" \
  --field "required_pull_request_reviews[dismiss_stale_reviews]=true" \
  --field "enforce_admins=false" \
  --field "restrictions=null" > /dev/null

echo -e "  ${GREEN}✅ develop — check '$CHECK_NAME' (app_id=$GHA_APP_ID)${NC}"

# ─── main ─────────────────────────────────────────────────────────────────────
echo "▶ main..."
gh api --method PUT "repos/$REPO/branches/main/protection" \
  --field "required_status_checks[strict]=true" \
  --field "required_status_checks[checks][][context]=$CHECK_NAME" \
  --field "required_status_checks[checks][][app_id]=$GHA_APP_ID" \
  --field "required_pull_request_reviews[required_approving_review_count]=1" \
  --field "required_pull_request_reviews[dismiss_stale_reviews]=true" \
  --field "enforce_admins=false" \
  --field "restrictions=null" > /dev/null

echo -e "  ${GREEN}✅ main — check '$CHECK_NAME' (app_id=$GHA_APP_ID)${NC}"

echo -e "\n${GREEN}Branch protection configurada correctamente.${NC}"
echo "Los PRs ahora reconocerán el check de GitHub Actions sin bloqueos."
echo ""
echo "Nota: si cambias el nombre del workflow o del job en ci.yml,"
echo "actualiza CHECK_NAME en este script y vuelve a ejecutarlo."
echo ""

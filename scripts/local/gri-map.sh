#!/usr/bin/env bash
set -euo pipefail

# generate-repository-implementation-map-from-openapi.sh
#
# Generates a repository implementation map using OpenAPI as the expected contract
# and the backend repository as the implementation source of truth.
#
# Intended use:
#   ./generate-repository-implementation-map-from-openapi.sh \
#     --openapi ./openapi-evidata-backend-v1.6.1.yaml \
#     --repo . \
#     --output ./repository-map
#
# Notes:
# - Frontend is assumed to not exist yet unless --include-frontend is provided.
# - The script does not assume any database engine.
# - It does not require yq/jq. It uses Python stdlib and PyYAML only if available.
# - If PyYAML is not installed and the OpenAPI file is YAML, it falls back to a lightweight parser.

OPENAPI_FILE=""
REPO_DIR="."
OUT_DIR="./repository-implementation-map-output"
INCLUDE_FRONTEND="false"
FRONTEND_DIR=""
PACKAGE_VERSION="v1.6.1"

usage() {
  cat <<'EOF'
Usage:
  generate-repository-implementation-map-from-openapi.sh --openapi <file> [--repo <dir>] [--output <dir>] [--include-frontend <dir>] [--version <version>]

Examples:
  ./generate-repository-implementation-map-from-openapi.sh \
    --openapi ./openapi-evidata-backend-v1.6.1.yaml \
    --repo . \
    --output ./repository-map

  ./generate-repository-implementation-map-from-openapi.sh \
    --openapi ./docs/openapi-evidata-backend-v1.6.1.json \
    --repo ./backend \
    --include-frontend ./frontend \
    --output ./repository-map

Options:
  --openapi <file>          Required. OpenAPI JSON or YAML file.
  --repo <dir>              Backend repository root. Default: current directory.
  --output <dir>            Output directory. Default: ./repository-implementation-map-output
  --include-frontend <dir>  Optional. Frontend repo root if it already exists.
  --version <version>       Documentation version label. Default: v1.6.1
  -h, --help                Show this help.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --openapi)
      OPENAPI_FILE="${2:-}"; shift 2 ;;
    --repo)
      REPO_DIR="${2:-}"; shift 2 ;;
    --output)
      OUT_DIR="${2:-}"; shift 2 ;;
    --include-frontend)
      INCLUDE_FRONTEND="true"; FRONTEND_DIR="${2:-}"; shift 2 ;;
    --version)
      PACKAGE_VERSION="${2:-}"; shift 2 ;;
    -h|--help)
      usage; exit 0 ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1 ;;
  esac
done

if [[ -z "$OPENAPI_FILE" ]]; then
  echo "ERROR: --openapi is required." >&2
  usage
  exit 1
fi

if [[ ! -f "$OPENAPI_FILE" ]]; then
  echo "ERROR: OpenAPI file not found: $OPENAPI_FILE" >&2
  exit 1
fi

if [[ ! -d "$REPO_DIR" ]]; then
  echo "ERROR: repo directory not found: $REPO_DIR" >&2
  exit 1
fi

if [[ "$INCLUDE_FRONTEND" == "true" && ! -d "$FRONTEND_DIR" ]]; then
  echo "ERROR: frontend directory not found: $FRONTEND_DIR" >&2
  exit 1
fi

mkdir -p "$OUT_DIR"
INV_DIR="$OUT_DIR/repository-inventory"
mkdir -p "$INV_DIR"

# Resolve paths where possible.
OPENAPI_ABS="$(cd "$(dirname "$OPENAPI_FILE")" && pwd)/$(basename "$OPENAPI_FILE")"
REPO_ABS="$(cd "$REPO_DIR" && pwd)"
OUT_ABS="$(cd "$OUT_DIR" && pwd)"

printf "Generating repository implementation map...\n"
printf "OpenAPI: %s\n" "$OPENAPI_ABS"
printf "Backend repo: %s\n" "$REPO_ABS"
printf "Output: %s\n" "$OUT_ABS"

# ----------------------------
# 1. Repository inventories
# ----------------------------
(
  cd "$REPO_ABS"
  find . -type f \
    \( -name "*.cs" -o -name "*.csproj" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.md" \) \
    | sort > "$OUT_ABS/repository-inventory/backend-repo-files.txt"

  grep -RIn \
    "ProcessingActivity\|Treatment\|Evidence\|Gap\|Review\|AuditEvent\|TimelineEvent\|Outbox\|ExportRequest\|processing-activities\|treatments" \
    --include="*.cs" \
    --include="*.json" \
    --include="*.yml" \
    --include="*.yaml" \
    . > "$OUT_ABS/repository-inventory/backend-symbol-search.txt" || true

  grep -RIn \
    "HttpTrigger\|Route\s*=\|MapGet\|MapPost\|MapPatch\|MapDelete\|processing-activities\|treatments" \
    --include="*.cs" \
    . > "$OUT_ABS/repository-inventory/backend-api-routes.txt" || true

  grep -RIn \
    "class ProcessingActivity\|record ProcessingActivity\|class Evidence\|class Gap\|class Review\|class AuditEvent\|class ExportRequest" \
    --include="*.cs" \
    . > "$OUT_ABS/repository-inventory/backend-domain-entities.txt" || true

  grep -RIn \
    "class Treatment\|record Treatment\|TreatmentVersion\|TreatmentGap\|TreatmentEvidence\|TreatmentReview\|/api/v1/treatments\|api/v1/treatments" \
    --include="*.cs" \
    --include="*.json" \
    --include="*.yml" \
    --include="*.yaml" \
    . > "$OUT_ABS/repository-inventory/backend-prohibited-treatment-symbols.txt" || true

  grep -RIn \
    "ViewModel\|Dto\|Request\|Response\|ProcessingActivityControlViewModel\|ProcessingActivityListViewModel\|ResourcePermissionsViewModel\|BlockedActionViewModel\|ApiErrorResponse" \
    --include="*.cs" \
    . > "$OUT_ABS/repository-inventory/backend-contracts-viewmodels.txt" || true

  grep -RIn \
    "Authorize\|Authorization\|Permission\|Role\|RBAC\|Policy\|TenantOwner\|ComplianceAdmin\|ProcessOwner\|LegalReviewer\|SecurityReviewer\|Auditor\|Viewer" \
    --include="*.cs" \
    . > "$OUT_ABS/repository-inventory/backend-authorization-search.txt" || true

  grep -RIn \
    "AuditEvent\|TimelineEvent\|Outbox\|DomainEvent\|CorrelationId\|OccurredAt" \
    --include="*.cs" \
    . > "$OUT_ABS/repository-inventory/backend-audit-events-search.txt" || true

  find . -type f \
    \( -name "*Tests.cs" -o -name "*.Tests.csproj" -o -name "*.IntegrationTests.csproj" \) \
    | sort > "$OUT_ABS/repository-inventory/backend-test-files.txt"

  grep -RIn \
    "Fact\|Theory\|TestMethod\|TestClass\|Integration\|Contract\|Authorize\|Audit\|ProcessingActivity\|Evidence\|Gap\|WebApplicationFactory\|TestServer" \
    --include="*.cs" \
    . > "$OUT_ABS/repository-inventory/backend-tests-search.txt" || true
)

if [[ "$INCLUDE_FRONTEND" == "true" ]]; then
  FRONTEND_ABS="$(cd "$FRONTEND_DIR" && pwd)"
  (
    cd "$FRONTEND_ABS"
    find . -type f \
      \( -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.json" -o -name "*.md" \) \
      | sort > "$OUT_ABS/repository-inventory/frontend-repo-files.txt"

    grep -RIn \
      "treatments\|processing-activities\|ProcessingActivity\|TreatmentControl\|Control del tratamiento\|Evidence\|Gap\|Review\|Audit\|Timeline\|Export\|@playwright/test\|data-testid" \
      --include="*.ts" \
      --include="*.tsx" \
      --include="*.json" \
      . > "$OUT_ABS/repository-inventory/frontend-symbol-search.txt" || true

    find . -type f \
      \( -name "*.test.ts" -o -name "*.test.tsx" -o -name "*.spec.ts" -o -name "*.spec.tsx" \) \
      | sort > "$OUT_ABS/repository-inventory/frontend-test-files.txt"
  )
else
  cat > "$OUT_ABS/repository-inventory/frontend-not-analyzed.txt" <<EOF
Frontend was not analyzed.
Reason: frontend does not exist yet or --include-frontend was not provided.
All frontend artifacts should be classified as New in repository-implementation-map.md.
EOF
fi

# ----------------------------
# 2. OpenAPI extraction + map generation
# ----------------------------
python3 - "$OPENAPI_ABS" "$REPO_ABS" "$OUT_ABS" "$PACKAGE_VERSION" "$INCLUDE_FRONTEND" <<'PY'
import csv
import json
import os
import re
import sys
from pathlib import Path
from datetime import datetime, timezone

openapi_path = Path(sys.argv[1])
repo_dir = Path(sys.argv[2])
out_dir = Path(sys.argv[3])
package_version = sys.argv[4]
include_frontend = sys.argv[5].lower() == "true"

HTTP_METHODS = {"get", "post", "put", "patch", "delete", "head", "options", "trace"}


def load_openapi(path: Path):
    text = path.read_text(encoding="utf-8", errors="replace")
    if path.suffix.lower() == ".json":
        return json.loads(text)
    # Prefer PyYAML when available.
    try:
        import yaml  # type: ignore
        return yaml.safe_load(text)
    except Exception:
        pass
    # Lightweight fallback parser for common OpenAPI YAML shape.
    # It extracts paths/methods/operationId/security/x-change-status and simple schemas.
    data = {"paths": {}, "components": {"schemas": {}}}
    current_path = None
    current_method = None
    in_paths = False
    in_schemas = False
    for raw in text.splitlines():
        line = raw.rstrip("\n")
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        indent = len(line) - len(line.lstrip(" "))
        if indent == 0 and stripped == "paths:":
            in_paths = True
            in_schemas = False
            continue
        if indent == 0 and stripped == "components:":
            in_paths = False
            current_path = None
            current_method = None
            continue
        if stripped == "schemas:" and indent <= 4:
            in_schemas = True
            in_paths = False
            continue
        if in_paths:
            m_path = re.match(r"^\s{2}(['\"]?)(/[^:'\"]+)\1:\s*$", line)
            if m_path:
                current_path = m_path.group(2)
                data["paths"].setdefault(current_path, {})
                current_method = None
                continue
            m_method = re.match(r"^\s{4}(get|post|put|patch|delete|head|options|trace):\s*$", line)
            if m_method and current_path:
                current_method = m_method.group(1)
                data["paths"][current_path].setdefault(current_method, {})
                continue
            if current_path and current_method:
                m_op = re.match(r"^\s{6}operationId:\s*['\"]?([^'\"]+)['\"]?\s*$", line)
                if m_op:
                    data["paths"][current_path][current_method]["operationId"] = m_op.group(1).strip()
                    continue
                m_x = re.match(r"^\s{6}x-change-status:\s*['\"]?([^'\"]+)['\"]?\s*$", line)
                if m_x:
                    data["paths"][current_path][current_method]["x-change-status"] = m_x.group(1).strip()
                    continue
                if re.match(r"^\s{6}security:\s*", line):
                    data["paths"][current_path][current_method]["security"] = [{}]
                    continue
        elif in_schemas:
            m_schema = re.match(r"^\s{4}([A-Za-z0-9_.-]+):\s*$", line)
            if m_schema:
                data["components"]["schemas"].setdefault(m_schema.group(1), {})
    return data


def read_file(path: Path):
    if not path.exists():
        return ""
    return path.read_text(encoding="utf-8", errors="replace")


def normalize_path_for_search(path: str):
    # /api/v1/processing-activities/{id}/control -> patterns for common route declarations.
    no_leading = path.lstrip("/")
    route_variants = {path, no_leading}
    # Replace OpenAPI params with common route param variants.
    route_variants.add(re.sub(r"\{([^}]+)\}", r"{\1}", no_leading))
    route_variants.add(re.sub(r"\{([^}]+)\}", r":\1", no_leading))
    route_variants.add(re.sub(r"\{([^}]+)\}", r"<\1>", no_leading))
    # Also search segment without api/v1 prefix for Functions that use route prefix globally.
    if no_leading.startswith("api/v1/"):
        route_variants.add(no_leading[len("api/v1/"):])
    return sorted(route_variants, key=len, reverse=True)


def endpoint_regex(path: str):
    # Broad matching regex: api/v1/processing-activities/{id}/control with flexible params.
    p = path.strip("/")
    parts = p.split("/")
    regex_parts = []
    for part in parts:
        if part.startswith("{") and part.endswith("}"):
            regex_parts.append(r"(?:\{[^/]+\}|:[A-Za-z0-9_]+|<[^/]+>|[A-Za-z0-9_]+)")
        else:
            regex_parts.append(re.escape(part))
    full = r"/".join(regex_parts)
    return re.compile(full, re.IGNORECASE)


def grep_hits(patterns, extensions=(".cs", ".json", ".yml", ".yaml"), max_hits=8):
    hits = []
    compiled = []
    for p in patterns:
        if hasattr(p, "search"):
            compiled.append(p)
        else:
            compiled.append(re.compile(re.escape(str(p)), re.IGNORECASE))
    for root, _, files in os.walk(repo_dir):
        # Skip common heavy dirs.
        if any(skip in root for skip in ["/.git", "/bin", "/obj", "/node_modules", "/.next", "/dist", "/build"]):
            continue
        for fn in files:
            path = Path(root) / fn
            if path.suffix.lower() not in extensions:
                continue
            try:
                for idx, line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
                    for rx in compiled:
                        if rx.search(line):
                            rel = path.relative_to(repo_dir)
                            hits.append(f"{rel}:{idx}: {line.strip()[:180]}")
                            if len(hits) >= max_hits:
                                return hits
                            break
            except Exception:
                continue
    return hits


def classify_endpoint(path, method, operation_id, hits):
    if path.startswith("/api/v1/treatments") or "/api/v1/treatments" in path:
        return "NotAllowed"
    if hits:
        # We still call ExistingModify because existence does not prove behavior matches OpenAPI.
        return "ExistingModify"
    return "New"


def classify_schema(schema_name, hits):
    if schema_name in {"Treatment", "TreatmentVersion", "TreatmentGap", "TreatmentEvidence", "TreatmentReview"}:
        return "NotAllowed" if hits else "NotAllowed"
    if hits:
        return "ExistingModify"
    return "New"


def md_escape(value):
    if value is None:
        return ""
    return str(value).replace("|", "\\|").replace("\n", " ")


def first_evidence(hits):
    if not hits:
        return "—"
    return "<br>".join(md_escape(h) for h in hits[:3])

api = load_openapi(openapi_path)
paths = api.get("paths", {}) or {}
schemas = (((api.get("components") or {}).get("schemas") or {}) if isinstance(api, dict) else {})

endpoints = []
for path, item in paths.items():
    if not isinstance(item, dict):
        continue
    for method, op in item.items():
        if method.lower() not in HTTP_METHODS:
            continue
        if not isinstance(op, dict):
            op = {}
        operation_id = op.get("operationId", "")
        change_status = op.get("x-change-status", "")
        has_security = "yes" if op.get("security") or api.get("security") else "no"
        patterns = normalize_path_for_search(path) + [endpoint_regex(path)]
        if operation_id:
            # Add operationId and PascalCase variant.
            pascal = re.sub(r"(^|[_\-\s]+)([a-zA-Z])", lambda m: m.group(2).upper(), operation_id)
            patterns.extend([operation_id, pascal])
        hits = grep_hits(patterns, max_hits=8)
        classification = classify_endpoint(path, method.upper(), operation_id, hits)
        endpoints.append({
            "method": method.upper(),
            "path": path,
            "operationId": operation_id,
            "xChangeStatus": change_status,
            "security": has_security,
            "classification": classification,
            "hits": hits,
        })

schema_rows = []
for schema_name in sorted(schemas.keys()):
    patterns = [f"class {schema_name}", f"record {schema_name}", schema_name]
    hits = grep_hits(patterns, extensions=(".cs", ".json", ".yml", ".yaml"), max_hits=8)
    schema_rows.append({
        "schema": schema_name,
        "classification": classify_schema(schema_name, hits),
        "hits": hits,
    })

# Extra expected domain artifacts from documentation.
expected_domain = [
    "ProcessingActivity", "ProcessingActivityVersion", "ProcessingActivityNode",
    "ProcessingActivityTemplate", "ProcessingActivityTemplateVersion",
    "EvidenceRequirement", "Evidence", "EvidenceAssociation", "EvidenceValidation",
    "Gap", "GapRule", "GapResolution",
    "Review", "ReviewDecision",
    "ExportRequest", "ExportFile",
    "AuditEvent", "TimelineEvent", "OutboxMessage",
    "Tenant", "User", "Role", "Permission",
]

domain_rows = []
for artifact in expected_domain:
    hits = grep_hits([f"class {artifact}", f"record {artifact}", artifact], max_hits=8)
    domain_rows.append({
        "artifact": artifact,
        "classification": "ExistingModify" if hits else "New",
        "hits": hits,
    })

prohibited_patterns = [
    "class Treatment", "record Treatment", "TreatmentVersion", "TreatmentGap",
    "TreatmentEvidence", "TreatmentReview", "/api/v1/treatments", "api/v1/treatments"
]
prohibited_hits = grep_hits(prohibited_patterns, extensions=(".cs", ".json", ".yml", ".yaml", ".md"), max_hits=50)

# Tests and QA capabilities.
test_capabilities = [
    ("Backend unit tests", ["[Fact]", "[Theory]", "TestMethod", "TestClass"]),
    ("Backend integration tests", ["Integration", "WebApplicationFactory", "TestServer", "IntegrationTests"]),
    ("OpenAPI contract tests", ["OpenAPI", "Swagger", "contract", "ApiErrorResponse"]),
    ("Authorization/security tests", ["Authorize", "Authorization", "Permission", "RBAC", "Forbidden", "403"]),
    ("Audit tests", ["AuditEvent", "TimelineEvent", "CorrelationId"]),
]

test_rows = []
for name, patterns in test_capabilities:
    hits = grep_hits(patterns, extensions=(".cs", ".json", ".yml", ".yaml", ".md"), max_hits=8)
    test_rows.append({"capability": name, "status": "FoundCandidates" if hits else "NotFound", "hits": hits})

# Write CSVs.
with (out_dir / "openapi-endpoints.csv").open("w", encoding="utf-8", newline="") as f:
    w = csv.writer(f)
    w.writerow(["method", "path", "operationId", "x-change-status", "security", "classification", "evidence"])
    for r in endpoints:
        w.writerow([r["method"], r["path"], r["operationId"], r["xChangeStatus"], r["security"], r["classification"], " || ".join(r["hits"][:5])])

with (out_dir / "openapi-schemas.csv").open("w", encoding="utf-8", newline="") as f:
    w = csv.writer(f)
    w.writerow(["schema", "classification", "evidence"])
    for r in schema_rows:
        w.writerow([r["schema"], r["classification"], " || ".join(r["hits"][:5])])

with (out_dir / "prohibited-routes-report.md").open("w", encoding="utf-8") as f:
    f.write("# Prohibited routes and backend Treatment symbols report\n\n")
    if prohibited_hits:
        f.write("## Findings\n\n")
        for h in prohibited_hits:
            f.write(f"- `{h}`\n")
    else:
        f.write("No prohibited backend Treatment symbols or `/api/v1/treatments` routes were found by static search.\n")

# Gap report.
with (out_dir / "implementation-gap-report.md").open("w", encoding="utf-8") as f:
    f.write("# Implementation gap report\n\n")
    f.write("## Endpoint gaps\n\n")
    f.write("| Method | Path | OperationId | Classification | Action |\n")
    f.write("|---|---|---|---|---|\n")
    for r in endpoints:
        if r["classification"] == "New":
            action = "Implement endpoint and tests from OpenAPI contract."
        elif r["classification"] == "ExistingModify":
            action = "Verify handler behavior, request/response schema, security, audit, and tests."
        elif r["classification"] == "NotAllowed":
            action = "Remove or block; backend route is prohibited."
        else:
            action = "Review."
        f.write(f"| {r['method']} | `{md_escape(r['path'])}` | `{md_escape(r['operationId'])}` | {r['classification']} | {action} |\n")
    f.write("\n## Schema gaps\n\n")
    f.write("| Schema | Classification | Action |\n")
    f.write("|---|---|---|\n")
    for r in schema_rows:
        action = "Create class/record or generated contract." if r["classification"] == "New" else "Verify fields and serialization match OpenAPI."
        f.write(f"| `{md_escape(r['schema'])}` | {r['classification']} | {action} |\n")

# Main repository implementation map.
now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")
with (out_dir / "repository-implementation-map.md").open("w", encoding="utf-8") as f:
    f.write(f"# Evidata — Repository Implementation Map\n\n")
    f.write(f"Document version: {package_version}\n\n")
    f.write(f"Generated at: {now}\n\n")
    f.write(f"OpenAPI source: `{openapi_path}`\n\n")
    f.write(f"Backend repository: `{repo_dir}`\n\n")
    if include_frontend:
        f.write("Frontend repository: analyzed.\n\n")
    else:
        f.write("Frontend repository: not analyzed because it does not exist yet or was not provided. All frontend artifacts remain `New`.\n\n")

    f.write("---\n\n")
    f.write("## 1. Purpose\n\n")
    f.write("This document maps Evidata documentation and OpenAPI contracts against the real backend repository. It is intended to prevent duplicate implementation, prohibited backend `Treatment` concepts, undocumented API drift, and mock-complacent quality validation.\n\n")

    f.write("## 2. Summary\n\n")
    total_endpoints = len(endpoints)
    endpoint_new = sum(1 for r in endpoints if r["classification"] == "New")
    endpoint_modify = sum(1 for r in endpoints if r["classification"] == "ExistingModify")
    endpoint_not_allowed = sum(1 for r in endpoints if r["classification"] == "NotAllowed")
    schema_new = sum(1 for r in schema_rows if r["classification"] == "New")
    schema_modify = sum(1 for r in schema_rows if r["classification"] == "ExistingModify")
    prohibited_count = len(prohibited_hits)
    f.write("| Category | Total | ExistingModify | New | NotAllowed / Findings |\n")
    f.write("|---|---:|---:|---:|---:|\n")
    f.write(f"| OpenAPI endpoints | {total_endpoints} | {endpoint_modify} | {endpoint_new} | {endpoint_not_allowed} |\n")
    f.write(f"| OpenAPI schemas | {len(schema_rows)} | {schema_modify} | {schema_new} | 0 |\n")
    f.write(f"| Prohibited backend findings | {prohibited_count} | 0 | 0 | {prohibited_count} |\n\n")

    f.write("## 3. OpenAPI endpoint implementation map\n\n")
    f.write("Classification is static-search based. `ExistingModify` means candidate implementation exists, but behavior must still be verified with integration, contract, security, and audit tests.\n\n")
    f.write("| Method | Path | OperationId | Security | x-change-status | Classification | Evidence | Required action |\n")
    f.write("|---|---|---|---|---|---|---|---|\n")
    for r in endpoints:
        if r["classification"] == "New":
            action = "Implement endpoint, handler, contract tests, integration tests, authorization, audit where applicable."
        elif r["classification"] == "ExistingModify":
            action = "Verify and align behavior to OpenAPI; add missing tests and audit/security checks."
        elif r["classification"] == "NotAllowed":
            action = "Remove/block; route is prohibited by Evidata backend naming rules."
        else:
            action = "Review."
        f.write(f"| {r['method']} | `{md_escape(r['path'])}` | `{md_escape(r['operationId'])}` | {r['security']} | `{md_escape(r['xChangeStatus'])}` | {r['classification']} | {first_evidence(r['hits'])} | {action} |\n")

    f.write("\n## 4. OpenAPI schema implementation map\n\n")
    f.write("| Schema | Classification | Evidence | Required action |\n")
    f.write("|---|---|---|---|\n")
    for r in schema_rows:
        action = "Create contract class/record or generated type." if r["classification"] == "New" else "Verify properties, enum values, serialization, nullable fields, and error behavior."
        f.write(f"| `{md_escape(r['schema'])}` | {r['classification']} | {first_evidence(r['hits'])} | {action} |\n")

    f.write("\n## 5. Domain artifact map\n\n")
    f.write("| Artifact | Classification | Evidence | Required action |\n")
    f.write("|---|---|---|---|\n")
    for r in domain_rows:
        action = "Create or map to existing equivalent." if r["classification"] == "New" else "Verify alignment with domain model v1.6.1."
        f.write(f"| `{md_escape(r['artifact'])}` | {r['classification']} | {first_evidence(r['hits'])} | {action} |\n")

    f.write("\n## 6. Prohibited backend symbols and routes\n\n")
    if prohibited_hits:
        f.write("The following findings require immediate review. Backend `Treatment` concepts and `/api/v1/treatments` routes are not allowed unless explicitly documented as frontend-only terms.\n\n")
        for h in prohibited_hits:
            f.write(f"- `{md_escape(h)}`\n")
    else:
        f.write("No prohibited backend `Treatment` symbols or `/api/v1/treatments` routes were found by static search.\n")

    f.write("\n## 7. Test capability map\n\n")
    f.write("| Capability | Status | Evidence | Required action |\n")
    f.write("|---|---|---|---|\n")
    for r in test_rows:
        action = "Create tests and CI gate." if r["status"] == "NotFound" else "Inspect quality; ensure tests are not mock-complacent."
        f.write(f"| {md_escape(r['capability'])} | {r['status']} | {first_evidence(r['hits'])} | {action} |\n")

    f.write("\n## 8. Frontend status\n\n")
    if include_frontend:
        f.write("Frontend repository was analyzed. See repository-inventory/frontend-* files for raw evidence.\n")
    else:
        f.write("Frontend does not exist yet in this analysis. Required classification: `New` for frontend app, routes, components, generated API client, Playwright setup, fixtures, and E2E tests.\n\n")
        f.write("| Artifact | Classification | Required action |\n")
        f.write("|---|---|---|\n")
        frontend_artifacts = [
            "/treatments route", "/treatments/:id/control route", "TreatmentControlPage",
            "OperationalMap", "TreatmentContextDrawer", "EvidenceRequirementList", "GapList",
            "ReviewPanel", "Generated OpenAPI client", "Playwright E2E setup", "Frontend fixtures",
        ]
        for art in frontend_artifacts:
            f.write(f"| `{art}` | New | Implement from frontend docs {package_version}; consume backend `/api/v1/processing-activities`, not `/api/v1/treatments`. |\n")

    f.write("\n## 9. Critical gaps and recommended next actions\n\n")
    f.write("| ID | Gap | Priority | Recommended action |\n")
    f.write("|---|---|---|---|\n")
    if endpoint_new:
        f.write(f"| GAP-API-001 | {endpoint_new} OpenAPI endpoint(s) not found by static search. | P0 | Implement or map to existing routes and add integration/contract tests. |\n")
    if schema_new:
        f.write(f"| GAP-CONTRACT-001 | {schema_new} OpenAPI schema(s) not found by static search. | P0 | Create/generated DTOs or map existing equivalents. |\n")
    if prohibited_count:
        f.write(f"| GAP-NAMING-001 | {prohibited_count} prohibited backend Treatment finding(s). | P0 | Remove, rename, or prove they are harmless frontend-only artifacts. |\n")
    if not include_frontend:
        f.write("| GAP-FE-001 | Frontend does not exist yet. | P1 | Create frontend app, generated API client, routes, components, Playwright setup. |\n")
    if not endpoint_new and not schema_new and not prohibited_count:
        f.write("| — | No critical static gaps detected. | — | Continue with behavior validation and test implementation. |\n")

    f.write("\n## 10. Closure checklist\n\n")
    checklist = [
        "No backend entity `Treatment` exists.",
        "No backend route `/api/v1/treatments` exists.",
        "OpenAPI endpoints are implemented or explicitly planned.",
        "OpenAPI schemas map to real DTOs/viewmodels or generated types.",
        "`ProcessingActivity` is the canonical backend aggregate or mapped equivalent.",
        "Critical actions use real authorization, not mock-complacent checks.",
        "Critical actions generate `AuditEvent` or mapped audit equivalent.",
        "Integration tests use real project persistence technology in test mode.",
        "Contract tests validate real backend responses against OpenAPI.",
        "Frontend, once created, consumes `/api/v1/processing-activities` only.",
        "Playwright E2E setup exists before closing critical user stories.",
        "CI gates block PR/main/release according to quality matrix.",
    ]
    for item in checklist:
        f.write(f"- [ ] {item}\n")

# Validation results.
with (out_dir / "VALIDATION-RESULTS-repository-map.txt").open("w", encoding="utf-8") as f:
    f.write("Repository implementation map validation\n")
    f.write("======================================\n\n")
    f.write(f"generated_at: {now}\n")
    f.write(f"openapi_file: {openapi_path}\n")
    f.write(f"package_version: {package_version}\n")
    f.write(f"openapi_parseable: OK\n")
    f.write(f"openapi_paths: {len(paths)}\n")
    f.write(f"openapi_operations: {len(endpoints)}\n")
    f.write(f"openapi_schemas: {len(schema_rows)}\n")
    f.write(f"openapi_no_api_v1_treatments_paths: {'OK' if not any(r['path'].startswith('/api/v1/treatments') for r in endpoints) else 'FAIL'}\n")
    f.write(f"prohibited_backend_findings: {prohibited_count}\n")
    f.write(f"endpoint_new_count: {endpoint_new}\n")
    f.write(f"endpoint_existing_modify_count: {endpoint_modify}\n")
    f.write(f"schema_new_count: {schema_new}\n")
    f.write(f"schema_existing_modify_count: {schema_modify}\n")
    f.write(f"frontend_analyzed: {'YES' if include_frontend else 'NO'}\n")

print(f"Generated {out_dir / 'repository-implementation-map.md'}")
print(f"Generated {out_dir / 'openapi-endpoints.csv'}")
print(f"Generated {out_dir / 'openapi-schemas.csv'}")
print(f"Generated {out_dir / 'implementation-gap-report.md'}")
print(f"Generated {out_dir / 'prohibited-routes-report.md'}")
PY

# ----------------------------
# 3. Zip output
# ----------------------------
(
  cd "$OUT_ABS/.."
  BASE="$(basename "$OUT_ABS")"
  zip -qr "$OUT_ABS/${BASE}.zip" "$BASE"
)

printf "\nDone.\n"
printf "Main document: %s/repository-implementation-map.md\n" "$OUT_ABS"
printf "Zip: %s/%s.zip\n" "$OUT_ABS" "$(basename "$OUT_ABS")"

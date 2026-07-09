# Implementation gap report

## Endpoint gaps

| Method | Path | OperationId | Classification | Action |
|---|---|---|---|---|
| GET | `/weatherforecast` | `GetWeatherForecast` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/audit/tenant/{tenantId}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/audit/tenant/{tenantId}/resource/{resource}/{resourceId}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/documents` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/documents/upload-url` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/evidence` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/evidence` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/evidence/{id}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/gaps` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/gaps` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/gaps/summary` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/users/{userId}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| DELETE | `/api/users/{userId}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/users/link` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/legal-sources` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/legal-obligations` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/mcp/query` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/mcp/interactions` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/mcp/metrics/feedback` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/mcp/metrics/hitl` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/mcp/interactions/{interactionId}/feedback` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/mcp/interactions/{interactionId}/verify-citations` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/processing-activities` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/processing-activities` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/processing-activities/{id}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/reports` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/reports` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/reports/{id}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/search` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/roles/users/{userId}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/roles/assign` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| DELETE | `/api/roles/remove` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| POST | `/api/tenants` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/tenants/{id}` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| PATCH | `/api/tenants/{id}/settings` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| PATCH | `/api/tenants/{id}/status` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/workflows` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |
| GET | `/api/workflows/pending` | `` | ExistingModify | Verify handler behavior, request/response schema, security, audit, and tests. |

## Schema gaps

| Schema | Classification | Action |
|---|---|---|
| `AssignRoleToUserCommand` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `ChangeTenantStatusRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CitationVerificationReport` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CitationVerificationResult` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CitationVerificationStatus` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `ComplianceGapDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CreateEvidenceRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CreateGapRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CreateProcessingActivityRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CreateReportRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `CreateTenantRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `DocumentDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `EvidenceDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `GapSummaryDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `LegalObligationDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `LegalSourceDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `LinkExternalIdentityCommand` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpCitationSourceType` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpFeedbackMetrics` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpFeedbackRating` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpFeedbackRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpHitlSummary` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpInteractionEntry` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpInteractionHistory` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpInteractionStatus` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpQueryApiRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpQueryResponse` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `McpRiskLevel` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `ProcessingActivityDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `RatContextReference` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `RemoveRoleFromUserCommand` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `ReportJobDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `SearchHistoryDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `SearchLogEntryDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `TenantDto` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `UpdateTenantSettingsRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `UploadUrlRequest` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `UploadUrlResponse` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `WeatherForecast` | ExistingModify | Verify fields and serialization match OpenAPI. |
| `WorkflowTaskDto` | ExistingModify | Verify fields and serialization match OpenAPI. |

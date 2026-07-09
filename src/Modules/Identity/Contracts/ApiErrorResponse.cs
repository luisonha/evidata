using System.Text.Json.Serialization;

namespace Evidata.Modules.Identity.Contracts;

public sealed record ApiErrorResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("labelKey")] string LabelKey,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("details")] Dictionary<string, object?>? Details);

public sealed record ApiErrorEnvelope(
    [property: JsonPropertyName("error")] ApiErrorResponse Error);

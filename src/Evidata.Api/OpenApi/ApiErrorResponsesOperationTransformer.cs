using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Evidata.Api.OpenApi;

internal sealed class ApiErrorResponsesOperationTransformer : IOpenApiOperationTransformer
{
    private static readonly IReadOnlyDictionary<string, string> DefaultErrorResponses = new Dictionary<string, string>
    {
        ["400"] = "Request inválido.",
        ["401"] = "No autenticado.",
        ["403"] = "Sin permiso.",
        ["404"] = "No encontrado.",
        ["409"] = "Conflicto de versión o concurrencia.",
        ["422"] = "Regla de negocio bloqueante.",
        ["500"] = "Error inesperado."
    };

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Responses ??= new OpenApiResponses();

        foreach (var response in DefaultErrorResponses)
        {
            if (operation.Responses.ContainsKey(response.Key))
            {
                continue;
            }

            operation.Responses[response.Key] = new OpenApiResponse
            {
                Description = response.Value,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchemaReference("ApiErrorResponse", context.Document, null)
                    }
                }
            };
        }

        return Task.CompletedTask;
    }
}

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Evidata.Api.OpenApi;

internal sealed class BearerSecurityRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();

        if (operation.Security.Any(ContainsBearerAuth))
        {
            return Task.CompletedTask;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearerAuth", context.Document, null)] = new List<string>()
        });

        return Task.CompletedTask;
    }

    private static bool ContainsBearerAuth(OpenApiSecurityRequirement requirement)
        => requirement.Keys.Any(key => key is OpenApiSecuritySchemeReference reference && string.Equals(reference.Reference.Id, "bearerAuth", StringComparison.Ordinal));
}

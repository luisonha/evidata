using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Evidata.Api.OpenApi;

internal sealed class ApiErrorSchemaDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        document.Components.Schemas["ApiErrorResponse"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string> { "error" },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["error"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Required = new HashSet<string> { "code", "labelKey", "correlationId" },
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["code"] = new OpenApiSchema { Type = JsonSchemaType.String },
                        ["labelKey"] = new OpenApiSchema { Type = JsonSchemaType.String },
                        ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                        ["correlationId"] = new OpenApiSchema { Type = JsonSchemaType.String },
                        ["details"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            AdditionalPropertiesAllowed = true,
                            AdditionalProperties = new OpenApiSchema()
                        }
                    }
                }
            }
        };

        return Task.CompletedTask;
    }
}

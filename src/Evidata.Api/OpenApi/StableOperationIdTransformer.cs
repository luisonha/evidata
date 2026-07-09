using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text;

namespace Evidata.Api.OpenApi;

internal sealed class StableOperationIdTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(operation.OperationId))
        {
            return Task.CompletedTask;
        }

        operation.OperationId = BuildOperationId(context);
        return Task.CompletedTask;
    }

    private static string BuildOperationId(OpenApiOperationTransformerContext context)
    {
        if (context.Description.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            var controllerName = controllerActionDescriptor.ControllerName;
            var actionName = string.IsNullOrWhiteSpace(controllerActionDescriptor.ActionName)
                ? controllerActionDescriptor.MethodInfo.Name
                : controllerActionDescriptor.ActionName;

            return $"{Sanitize(controllerName)}_{Sanitize(actionName)}";
        }

        var routeValues = context.Description.ActionDescriptor.RouteValues;
        routeValues.TryGetValue("controller", out var controller);
        routeValues.TryGetValue("action", out var action);

        controller ??= context.Description.GroupName ?? "Operation";
        action ??= $"{context.Description.HttpMethod}_{context.Description.RelativePath}";

        return $"{Sanitize(controller)}_{Sanitize(action)}";
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString().Trim('_');
    }
}

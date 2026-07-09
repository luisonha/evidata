using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace Evidata.Api.OpenApi;

internal sealed class ChangeStatusOperationTransformer : IOpenApiOperationTransformer
{
    private static readonly HashSet<string> ExistingModifyControllers =
    [
        "Evidata.Modules.ProcessingInventory.Api.ProcessingActivitiesController",
        "Evidata.Modules.Evidence.Api.EvidenceController",
        "Evidata.Modules.GapManagement.Api.GapsController",
        "Evidata.Modules.Workflow.Api.WorkflowController",
        "Evidata.Modules.Reporting.Api.ReportsController",
        "Evidata.Modules.Audit.Api.AuditController"
    ];

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var changeStatus = ResolveChangeStatus(context.Description.ActionDescriptor);
        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions["x-change-status"] = new JsonNodeExtension(JsonValue.Create(changeStatus)!);
        return Task.CompletedTask;
    }

    private static string ResolveChangeStatus(Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor actionDescriptor)
    {
        if (actionDescriptor is ControllerActionDescriptor controllerActionDescriptor &&
            controllerActionDescriptor.ControllerTypeInfo.FullName is { } controllerFullName &&
            ExistingModifyControllers.Contains(controllerFullName))
        {
            return "ExistingModify";
        }

        return "DecisionRequired";
    }
}

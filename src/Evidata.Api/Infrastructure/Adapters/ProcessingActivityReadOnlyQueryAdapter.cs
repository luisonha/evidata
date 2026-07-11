using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.Reporting.Application.Abstractions;

namespace Evidata.Api.Infrastructure.Adapters;

/// <summary>
/// Adapter to expose ProcessingActivity status to Reporting module.
/// Used for SEC-EXP-001: Validating approved state before export generation.
/// </summary>
public sealed class ProcessingActivityReadOnlyQueryAdapter(
    GetProcessingActivityQueryHandler queryHandler) : IProcessingActivityReadOnlyQueryService
{
    public async Task<string?> GetStatusAsync(
        Guid tenantId, 
        Guid processingActivityId, 
        CancellationToken ct = default)
    {
        var dto = await queryHandler.HandleAsync(tenantId, processingActivityId, ct);
        return dto?.Status?.ToString();
    }
}

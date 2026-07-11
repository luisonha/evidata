using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.Reporting.Application.Abstractions;

namespace Evidata.Functions.Reporting.Infrastructure.Adapters;

/// <summary>
/// Adapter to expose ProcessingActivity status to Reporting module.
/// Used for SEC-EXP-001: Validating approved state before export generation.
/// 
/// P1-017-ADAPTER: Each host (Evidata.Api, Evidata.Functions.Reporting)
/// maintains its own adapter implementation in Infrastructure/Adapters.
/// This avoids circular dependencies while maintaining a consistent interface
/// across hosts. The implementation is identical between hosts — any changes
/// should be synchronized.
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

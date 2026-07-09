using Evidata.Functions.SearchIndexing.Models;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Search.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.SearchIndexing.Handlers;

/// <summary>
/// Indexa un tratamiento RAT aprobado en el catálogo de búsqueda.
///
/// Solo indexa tratamientos en estado Approved (los borradores no son buscables).
/// Registra el evento en SearchQueryLog con source="system-index-rat".
/// </summary>
public class RatIndexingHandler
{
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    private readonly ProcessingInventoryDbContext _rat;
    private readonly ISearchQueryLogService _searchLog;
    private readonly ILogger<RatIndexingHandler> _logger;

    public RatIndexingHandler(
        ProcessingInventoryDbContext rat,
        ISearchQueryLogService searchLog,
        ILogger<RatIndexingHandler> logger)
    {
        _rat = rat;
        _searchLog = searchLog;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(RatIndexPayload payload, CancellationToken ct)
    {
        var activity = await _rat.ProcessingActivities
            .AsNoTracking()
            .Where(a => a.TenantId == payload.TenantId)
            .FirstOrDefaultAsync(a => a.Id == payload.ActivityId, ct);

        if (activity is null)
        {
            _logger.LogWarning(
                "⚠ ProcessingActivity {ActivityId} no encontrada en tenant {TenantId} — ignorando",
                payload.ActivityId, payload.TenantId);
            return false;
        }

        if (activity.Status != ProcessingActivityStatus.Approved)
        {
            _logger.LogInformation(
                "ProcessingActivity {ActivityId} en estado {Status} — solo se indexan aprobados, saltando",
                payload.ActivityId, activity.Status);
            return false;
        }

        var searchTerm = BuildSearchTerm(activity);

        await _searchLog.RecordAsync(
            tenantId: payload.TenantId,
            userId: SystemUserId,
            query: searchTerm,
            source: "system-index-rat",
            resultCount: 1,
            filtersJson: BuildFiltersJson(activity),
            ct: ct);

        _logger.LogInformation(
            "✅ RAT indexado: {ActivityId} '{Name}' LegalBasis={LegalBasis} Tenant={TenantId}",
            activity.Id, activity.Name, activity.Purpose?.LegalBasis, activity.TenantId);

        return true;
    }

    private static string BuildSearchTerm(ProcessingActivity activity)
    {
        var parts = new List<string> { activity.Name };
        if (!string.IsNullOrWhiteSpace(activity.Description))
            parts.Add(activity.Description);
        if (!string.IsNullOrWhiteSpace(activity.Controller))
            parts.Add(activity.Controller);
        if (!string.IsNullOrWhiteSpace(activity.Department))
            parts.Add(activity.Department);
        return string.Join(" ", parts);
    }

    private static string BuildFiltersJson(ProcessingActivity activity)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            activityId = activity.Id,
            department = activity.Department,
            legalBasis = activity.Purpose?.LegalBasis.ToString(),
            hasSensitiveData = activity.Flags.SensitiveData,
            entityType = "ProcessingActivity"
        });
    }
}

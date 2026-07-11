using Evidata.Modules.Reporting.Domain;
using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Reporting.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for Export aggregate.
/// </summary>
public sealed class ExportRepository(ReportingDbContext dbContext) : IExportRepository
{
    public async Task AddAsync(Export export, CancellationToken ct = default)
    {
        await dbContext.Exports.AddAsync(export, ct);
    }

    public async Task UpdateAsync(Export export, CancellationToken ct = default)
    {
        dbContext.Exports.Update(export);
        await Task.CompletedTask;
    }

    public async Task<Export?> GetByIdAsync(Guid exportId, CancellationToken ct = default)
    {
        return await dbContext.Exports
            .FirstOrDefaultAsync(e => e.Id == exportId, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<Export>> GetByProcessingActivityAsync(
        Guid processingActivityId, CancellationToken ct = default)
    {
        return await dbContext.Exports
            .Where(e => e.ProcessingActivityId == processingActivityId)
            .OrderByDescending(e => e.RequestedAt)
            .ToListAsync(ct);
    }

    public async Task<Export?> GetLatestByActivityAndTypeAsync(
        Guid processingActivityId, ExportType exportType, CancellationToken ct = default)
    {
        return await dbContext.Exports
            .Where(e => e.ProcessingActivityId == processingActivityId && e.ExportType == exportType)
            .OrderByDescending(e => e.Version)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Export?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        return await dbContext.Exports
            .FirstOrDefaultAsync(e => e.CorrelationId == correlationId, cancellationToken: ct);
    }

    public async Task<int> GetNextVersionAsync(
        Guid processingActivityId, ExportType exportType, CancellationToken ct = default)
    {
        var maxVersion = await dbContext.Exports
            .Where(e => e.ProcessingActivityId == processingActivityId && e.ExportType == exportType)
            .MaxAsync(e => (int?)e.Version, cancellationToken: ct) ?? 0;

        return maxVersion + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await dbContext.SaveChangesAsync(ct);
    }
}

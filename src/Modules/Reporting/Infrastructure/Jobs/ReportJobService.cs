using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Domain;
using Evidata.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Reporting.Infrastructure.Jobs;

/// <summary>
/// Implementación de <see cref="IReportJobService"/> con EF Core + PostgreSQL.
/// </summary>
public sealed class ReportJobService : IReportJobService
{
    private readonly ReportingDbContext _db;

    public ReportJobService(ReportingDbContext db)
    {
        _db = db;
    }

    public async Task<ReportJob> RequestAsync(
        Guid tenantId, ReportType reportType, string parameters,
        Guid requestedBy, CancellationToken ct = default)
    {
        var job = ReportJob.Create(tenantId, reportType, parameters, requestedBy);
        _db.ReportJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task StartAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await GetRequiredAsync(jobId, ct);
        job.Start();
        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid jobId, Guid artifactDocumentId, CancellationToken ct = default)
    {
        var job = await GetRequiredAsync(jobId, ct);
        job.Complete(artifactDocumentId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task FailAsync(Guid jobId, string errorMessage, CancellationToken ct = default)
    {
        var job = await GetRequiredAsync(jobId, ct);
        job.Fail(errorMessage);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ExpireStaleJobsAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        var stale = await _db.ReportJobs
            .Where(j => (j.Status == ReportJobStatus.Requested || j.Status == ReportJobStatus.Running)
                     && j.RequestedAt < cutoff)
            .ToListAsync(ct);

        foreach (var j in stale)
            j.Expire();

        if (stale.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public Task<ReportJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default) =>
        _db.ReportJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);

    public async Task<IReadOnlyList<ReportJob>> GetByTenantAsync(
        Guid tenantId, int limit = 20, CancellationToken ct = default)
    {
        return await _db.ReportJobs
            .Where(j => j.TenantId == tenantId)
            .OrderByDescending(j => j.RequestedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    private async Task<ReportJob> GetRequiredAsync(Guid jobId, CancellationToken ct)
    {
        return await _db.ReportJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new KeyNotFoundException($"ReportJob {jobId} no encontrado.");
    }
}

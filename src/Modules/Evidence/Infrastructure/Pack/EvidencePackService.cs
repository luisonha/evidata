using System.Text.Json;
using Azure.Storage.Queues;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Evidence.Infrastructure.Pack;

public class EvidencePackService : IEvidencePackService
{
    private readonly EvidenceDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly QueueClient _queue;
    private readonly ILogger<EvidencePackService> _logger;

    public EvidencePackService(
        EvidenceDbContext db,
        IBlobStorageService blobStorage,
        QueueClient queue,
        ILogger<EvidencePackService> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _queue = queue;
        _logger = logger;
    }

    public async Task<CreatePackResult> RequestPackAsync(
        Guid tenantId,
        Guid requestedBy,
        IEnumerable<Guid> evidenceIds,
        CancellationToken ct = default)
    {
        var ids = evidenceIds.Distinct().ToList();

        // Validar que todas las evidencias existen, son Active y pertenecen al tenant
        var found = await _db.Evidences
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId
                     && ids.Contains(e.Id)
                     && e.Status == EvidenceStatus.Active)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var missing = ids.Except(found).ToList();
        if (missing.Count > 0)
            throw new KeyNotFoundException(
                $"Las siguientes evidencias no existen, no son Active o no pertenecen al tenant: {string.Join(", ", missing)}");

        var job = EvidencePackJob.Create(tenantId, requestedBy, ids);
        _db.EvidencePackJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Encolar mensaje para la Azure Function
        var message = new EvidencePackMessage(tenantId, job.Id);
        var json = JsonSerializer.Serialize(message);
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        await _queue.SendMessageAsync(encoded, cancellationToken: ct);

        _logger.LogInformation(
            "EvidencePackJob {JobId} creado para tenant {TenantId} con {Count} evidencias",
            job.Id, tenantId, ids.Count);

        return new CreatePackResult(job.Id);
    }

    public async Task<PackStatusResult> GetStatusAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken ct = default)
    {
        var job = await _db.EvidencePackJobs
            .Where(j => j.TenantId == tenantId && j.Id == jobId)
            .FirstOrDefaultAsync(ct);

        if (job is null)
            throw new KeyNotFoundException($"EvidencePackJob {jobId} no encontrado en tenant {tenantId}.");

        string? downloadUrl = null;
        if (job.Status == EvidencePackStatus.Completed && job.ResultBlobPath is not null)
        {
            var sas = await _blobStorage.GenerateDownloadSasAsync(
                job.ResultBlobPath, expiresIn: TimeSpan.FromHours(1), ct: ct);
            downloadUrl = sas.DownloadUrl;
        }

        return new PackStatusResult(
            job.Id,
            job.Status,
            downloadUrl,
            job.ExpiresAt,
            job.ErrorMessage);
    }
}

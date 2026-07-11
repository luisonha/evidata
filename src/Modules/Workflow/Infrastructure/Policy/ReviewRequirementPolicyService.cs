using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Workflow.Infrastructure.Policy;

/// <summary>
/// Implementación de <see cref="IReviewRequirementPolicyService"/> con caching distribuido
/// para determinar si un tipo de revisión es requerido (bloqueante) para un tipo de entidad.
/// 
/// Comportamiento:
/// - Consulta ReviewRequirement en BD. Si existe, retorna IsRequired.
/// - Si no existe configuración, retorna true (conservador — mantiene MVP actual).
/// - Cache con key: "review-requirement:{tenantId}:{entityType}:{reviewType}"
/// </summary>
public sealed class ReviewRequirementPolicyService : IReviewRequirementPolicyService
{
    private readonly WorkflowDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ReviewRequirementPolicyService> _logger;
    
    private const int CacheTtlMinutes = 60;

    public ReviewRequirementPolicyService(
        WorkflowDbContext db,
        IDistributedCache cache,
        ILogger<ReviewRequirementPolicyService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsReviewRequiredAsync(
        Guid tenantId,
        string entityType,
        ReviewType reviewType,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("entityType cannot be null or empty.", nameof(entityType));

        entityType = entityType.Trim();
        
        // Construir clave de cache
        var cacheKey = GetCacheKey(tenantId, entityType, reviewType);
        
        // Intentar leer del cache
        var cachedValue = await _cache.GetStringAsync(cacheKey, ct);
        if (cachedValue != null)
        {
            _logger.LogDebug(
                "Cache hit for review requirement: Tenant={TenantId}, EntityType={EntityType}, ReviewType={ReviewType}",
                tenantId, entityType, reviewType);
            
            return cachedValue == "1"; // "1" = true, "0" = false
        }

        // Consultar BD
        var requirement = await _db.ReviewRequirements
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId
                  && r.EntityType == entityType
                  && r.ReviewType == reviewType,
                ct);

        // Si existe, usar IsRequired. Si no existe, conservador: true.
        var isRequired = requirement?.IsRequired ?? true;
        
        _logger.LogDebug(
            "Review requirement check: Tenant={TenantId}, EntityType={EntityType}, ReviewType={ReviewType}, IsRequired={IsRequired} (Configured={Configured})",
            tenantId, entityType, reviewType, isRequired, requirement != null);

        // Guardar en cache
        await _cache.SetStringAsync(
            cacheKey,
            isRequired ? "1" : "0",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheTtlMinutes)
            },
            ct);

        return isRequired;
    }

    public async Task<IReadOnlyList<ReviewRequirement>> GetAllForTenantAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _db.ReviewRequirements
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.EntityType)
            .ThenBy(r => r.ReviewType)
            .ToListAsync(ct);
    }

    public async Task<ReviewRequirement> SetRequirementAsync(
        Guid tenantId,
        ReviewType reviewType,
        string entityType,
        bool isRequired,
        Guid modifiedBy,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("entityType cannot be null or empty.", nameof(entityType));

        if (modifiedBy == Guid.Empty)
            throw new ArgumentException("modifiedBy cannot be empty.", nameof(modifiedBy));

        entityType = entityType.Trim();

        // Buscar configuración existente
        var existing = await _db.ReviewRequirements
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId
                  && r.EntityType == entityType
                  && r.ReviewType == reviewType,
                ct);

        ReviewRequirement requirement;
        if (existing != null)
        {
            // Actualizar
            existing.SetRequired(isRequired, modifiedBy);
            _db.ReviewRequirements.Update(existing);
            requirement = existing;
            
            _logger.LogInformation(
                "Updated review requirement: Tenant={TenantId}, EntityType={EntityType}, ReviewType={ReviewType}, IsRequired={IsRequired}",
                tenantId, entityType, reviewType, isRequired);
        }
        else
        {
            // Crear nueva
            requirement = ReviewRequirement.Create(tenantId, reviewType, entityType, isRequired, modifiedBy);
            _db.ReviewRequirements.Add(requirement);
            
            _logger.LogInformation(
                "Created new review requirement: Tenant={TenantId}, EntityType={EntityType}, ReviewType={ReviewType}, IsRequired={IsRequired}",
                tenantId, entityType, reviewType, isRequired);
        }

        await _db.SaveChangesAsync(ct);

        // Invalidar cache
        InvalidateCacheFor(tenantId, entityType, reviewType);

        return requirement;
    }

    public async Task DeleteRequirementAsync(
        Guid tenantId,
        ReviewType reviewType,
        string entityType,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("entityType cannot be null or empty.", nameof(entityType));

        entityType = entityType.Trim();

        var requirement = await _db.ReviewRequirements
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId
                  && r.EntityType == entityType
                  && r.ReviewType == reviewType,
                ct);

        if (requirement != null)
        {
            _db.ReviewRequirements.Remove(requirement);
            await _db.SaveChangesAsync(ct);
            
            _logger.LogInformation(
                "Deleted review requirement: Tenant={TenantId}, EntityType={EntityType}, ReviewType={ReviewType}",
                tenantId, entityType, reviewType);

            // Invalidar cache
            InvalidateCacheFor(tenantId, entityType, reviewType);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetCacheKey(Guid tenantId, string entityType, ReviewType reviewType)
        => $"review-requirement:{tenantId}:{entityType}:{(int)reviewType}";

    private void InvalidateCacheFor(Guid tenantId, string entityType, ReviewType reviewType)
    {
        var cacheKey = GetCacheKey(tenantId, entityType, reviewType);
        _cache.Remove(cacheKey);
    }
}

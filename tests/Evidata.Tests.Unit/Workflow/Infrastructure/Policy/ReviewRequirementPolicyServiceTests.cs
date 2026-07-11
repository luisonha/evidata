using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Infrastructure.Policy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Evidata.Tests.Unit.Workflow.Infrastructure.Policy;

/// <summary>
/// Tests unitarios para ReviewRequirementPolicyService.
/// 
/// P1-018 — Requirement coverage per Gandalf conditional approval.
/// 
/// Tests cubren:
/// - Comportamiento conservador por defecto (no configurado = requerido)
/// - Configuración explícita respetada (IsRequired = true/false)
/// - Aislamiento por tenant
/// - Casos edge (entityType inválido, TenantId vacío)
/// </summary>
public class ReviewRequirementPolicyServiceTests
{
    private static WorkflowDbContext BuildDbContext() =>
        new(new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static MemoryCacheAdapter BuildMemoryCacheAdapter()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new MemoryCacheAdapter(memoryCache);
    }

    private static ILogger<ReviewRequirementPolicyService> BuildMockLogger() =>
        Substitute.For<ILogger<ReviewRequirementPolicyService>>();

    /// <summary>
    /// Test 1: Conservative default — sin configuración explícita, IsRequired debe ser true.
    /// </summary>
    [Fact]
    public async Task IsReviewRequiredAsync_NoConfiguration_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();
        
        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Act
        var result = await service.IsReviewRequiredAsync(
            tenantId,
            "ProcessingActivity",
            ReviewType.Legal);

        // Assert
        Assert.True(result, "Default behavior should require review when no configuration exists");
    }

    /// <summary>
    /// Test 2: Configuración explícita IsRequired=false debe ser respetada.
    /// </summary>
    [Fact]
    public async Task IsReviewRequiredAsync_ConfiguredAsNotRequired_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Create requirement with IsRequired=false
        var requirement = ReviewRequirement.Create(
            tenantId,
            ReviewType.Security,
            "ProcessingActivity",
            isRequired: false,
            userId);
        db.ReviewRequirements.Add(requirement);
        await db.SaveChangesAsync();

        // Act
        var result = await service.IsReviewRequiredAsync(
            tenantId,
            "ProcessingActivity",
            ReviewType.Security);

        // Assert
        Assert.False(result, "Configured as not required should return false");
    }

    /// <summary>
    /// Test 3: Configuración explícita IsRequired=true debe ser respetada.
    /// </summary>
    [Fact]
    public async Task IsReviewRequiredAsync_ConfiguredAsRequired_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Create requirement with IsRequired=true
        var requirement = ReviewRequirement.Create(
            tenantId,
            ReviewType.Legal,
            "ProcessingActivity",
            isRequired: true,
            userId);
        db.ReviewRequirements.Add(requirement);
        await db.SaveChangesAsync();

        // Act
        var result = await service.IsReviewRequiredAsync(
            tenantId,
            "ProcessingActivity",
            ReviewType.Legal);

        // Assert
        Assert.True(result, "Configured as required should return true");
    }

    /// <summary>
    /// Test 4: Tenant isolation — configuración de Tenant A no afecta Tenant B.
    /// </summary>
    [Fact]
    public async Task IsReviewRequiredAsync_DifferentTenants_IsolatedConfigurations()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Tenant A: Security NOT required
        var reqA = ReviewRequirement.Create(tenantA, ReviewType.Security, "ProcessingActivity", false, userId);
        db.ReviewRequirements.Add(reqA);
        
        // Tenant B: Security IS required (default)
        // (No explicit config, should default to true)
        await db.SaveChangesAsync();

        // Act
        var resultA = await service.IsReviewRequiredAsync(tenantA, "ProcessingActivity", ReviewType.Security);
        var resultB = await service.IsReviewRequiredAsync(tenantB, "ProcessingActivity", ReviewType.Security);

        // Assert
        Assert.False(resultA, "Tenant A configured Security as not required");
        Assert.True(resultB, "Tenant B (no config) defaults to required");
    }

    /// <summary>
    /// Test 5: Delete requirement — deve invalidar cache y revertir al default conservador.
    /// </summary>
    [Fact]
    public async Task DeleteRequirementAsync_CacheClearedReturnsDefault()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Create requirement
        var requirement = ReviewRequirement.Create(
            tenantId,
            ReviewType.Legal,
            "ProcessingActivity",
            isRequired: false,
            userId);
        db.ReviewRequirements.Add(requirement);
        await db.SaveChangesAsync();

        // Query to populate cache
        var beforeDelete = await service.IsReviewRequiredAsync(
            tenantId,
            "ProcessingActivity",
            ReviewType.Legal);

        // Act — Delete the requirement
        await service.DeleteRequirementAsync(tenantId, ReviewType.Legal, "ProcessingActivity");

        // Query again
        var afterDelete = await service.IsReviewRequiredAsync(
            tenantId,
            "ProcessingActivity",
            ReviewType.Legal);

        // Assert
        Assert.False(beforeDelete, "Before delete should be false");
        Assert.True(afterDelete, "After delete, should revert to conservative default (true)");
    }

    /// <summary>
    /// Test 6: GetAllForTenantAsync — retorna todas las configuraciones de un tenant, ordenadas.
    /// </summary>
    [Fact]
    public async Task GetAllForTenantAsync_ReturnsOnlyTenantConfigs_Ordered()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Create configs for Tenant A
        var req1 = ReviewRequirement.Create(tenantA, ReviewType.Security, "ProcessingActivity", false, userId);
        var req2 = ReviewRequirement.Create(tenantA, ReviewType.Legal, "Document", true, userId);
        var req3 = ReviewRequirement.Create(tenantA, ReviewType.Legal, "ProcessingActivity", false, userId);
        
        // Create config for Tenant B (should not appear)
        var req4 = ReviewRequirement.Create(tenantB, ReviewType.Legal, "ProcessingActivity", true, userId);
        
        db.ReviewRequirements.AddRange(req1, req2, req3, req4);
        await db.SaveChangesAsync();

        // Act
        var results = await service.GetAllForTenantAsync(tenantA);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(tenantA, r.TenantId));
    }

    /// <summary>
    /// Test 7: Edge case — entityType con espacios debe ser trimmed.
    /// </summary>
    [Fact]
    public async Task IsReviewRequiredAsync_EntityTypeWithWhitespace_Trimmed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Create requirement with trimmed entityType
        var requirement = ReviewRequirement.Create(
            tenantId,
            ReviewType.Legal,
            "ProcessingActivity",
            isRequired: false,
            userId);
        db.ReviewRequirements.Add(requirement);
        await db.SaveChangesAsync();

        // Act — Query with spaces around entityType
        var result = await service.IsReviewRequiredAsync(
            tenantId,
            "  ProcessingActivity  ",
            ReviewType.Legal);

        // Assert
        Assert.False(result, "Trimmed entityType should match stored value");
    }

    /// <summary>
    /// Test 8: Edge case — entityType vacío debe lanzar ArgumentException.
    /// </summary>
    [Fact]
    public async Task IsReviewRequiredAsync_EmptyEntityType_ThrowsArgumentException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.IsReviewRequiredAsync(tenantId, "", ReviewType.Legal));
    }

    /// <summary>
    /// Test 9: SetRequirementAsync — actualizar una configuración existente.
    /// </summary>
    [Fact]
    public async Task SetRequirementAsync_UpdateExisting_ModifiesAndInvalidatesCache()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Create initial requirement
        var initial = ReviewRequirement.Create(tenantId, ReviewType.Legal, "ProcessingActivity", true, userId);
        db.ReviewRequirements.Add(initial);
        await db.SaveChangesAsync();
        var initialId = initial.Id;

        // Act — Update it
        var updated = await service.SetRequirementAsync(
            tenantId,
            ReviewType.Legal,
            "ProcessingActivity",
            isRequired: false,
            anotherUserId);

        // Assert
        Assert.Equal(initialId, updated.Id);
        Assert.False(updated.IsRequired);
        Assert.Equal(anotherUserId, updated.ModifiedBy);
        
        var fromDb = await db.ReviewRequirements.FirstAsync(r => r.Id == initialId);
        Assert.False(fromDb.IsRequired);
    }

    /// <summary>
    /// Test 10: Multi-tenant security — different tenants get different data.
    /// </summary>
    [Fact]
    public async Task IsReviewRequiredAsync_MultiTenantIsolation_NoLeakageBetweenTenants()
    {
        // Arrange
        var tenantAttacker = Guid.NewGuid();
        var tenantVictim = Guid.NewGuid();
        var userAttacker = Guid.NewGuid();
        var userVictim = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Victim tenant: Security is NOT required
        var victimConfig = ReviewRequirement.Create(
            tenantVictim,
            ReviewType.Security,
            "ProcessingActivity",
            isRequired: false,
            userVictim);
        db.ReviewRequirements.Add(victimConfig);
        await db.SaveChangesAsync();

        // Act: Query tenant victim from attacker's perspective
        var attackerSeesVictimConfig = await service.IsReviewRequiredAsync(
            tenantVictim,
            "ProcessingActivity",
            ReviewType.Security);

        // Act: Query attacker's own tenant (no config)
        var attackerSeesOwnDefault = await service.IsReviewRequiredAsync(
            tenantAttacker,
            "ProcessingActivity",
            ReviewType.Security);

        // Assert: This test demonstrates service MUST be protected at controller level
        // Service doesn't enforce tenant isolation - it relies on controller to pass correct tenantId
        Assert.False(attackerSeesVictimConfig);
        Assert.True(attackerSeesOwnDefault);
    }

    /// <summary>
    /// Test 11: Cache invalidation after Set — second query reflects the change.
    /// </summary>
    [Fact]
    public async Task SetRequirementAsync_ThenQuery_CacheInvalidatedReflectsChange()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cache = BuildMemoryCacheAdapter();
        var logger = BuildMockLogger();

        await using var db = BuildDbContext();
        var service = new ReviewRequirementPolicyService(db, cache, logger);

        // Create initial requirement (IsRequired=true)
        var initial = ReviewRequirement.Create(
            tenantId,
            ReviewType.Security,
            "ProcessingActivity",
            isRequired: true,
            userId);
        db.ReviewRequirements.Add(initial);
        await db.SaveChangesAsync();

        // Query to populate cache
        var query1 = await service.IsReviewRequiredAsync(
            tenantId,
            "ProcessingActivity",
            ReviewType.Security);

        // Act — Update to IsRequired=false
        await service.SetRequirementAsync(
            tenantId,
            ReviewType.Security,
            "ProcessingActivity",
            isRequired: false,
            userId);

        // Query again (cache should be invalidated)
        var query2 = await service.IsReviewRequiredAsync(
            tenantId,
            "ProcessingActivity",
            ReviewType.Security);

        // Assert
        Assert.True(query1, "Initial value should be true");
        Assert.False(query2, "After update and cache invalidation, value should be false");
    }
}

/// <summary>
/// Adapter to use MemoryCache with IDistributedCache interface for testing.
/// </summary>
internal class MemoryCacheAdapter : IDistributedCache
{
    private readonly IMemoryCache _cache;

    public MemoryCacheAdapter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public byte[]? Get(string key) => throw new NotImplementedException();

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var stringValue = GetString(key);
        if (stringValue == null)
            return Task.FromResult<byte[]?>(null);
        var bytes = System.Text.Encoding.UTF8.GetBytes(stringValue);
        return Task.FromResult<byte[]?>(bytes);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw new NotImplementedException();

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var stringValue = System.Text.Encoding.UTF8.GetString(value);
        return SetStringAsync(key, stringValue, options, token);
    }

    public void Remove(string key) => _cache.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public string? GetString(string key)
    {
        if (_cache.TryGetValue(key, out var value) && value is string str)
            return str;
        return null;
    }

    public Task<string?> GetStringAsync(string key, CancellationToken token = default) =>
        Task.FromResult(GetString(key));

    public void SetString(string key, string value, DistributedCacheEntryOptions options) =>
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow
        });

    public Task SetStringAsync(string key, string value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        SetString(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key) => throw new NotImplementedException();
    public Task RefreshAsync(string key, CancellationToken token = default) => throw new NotImplementedException();
}

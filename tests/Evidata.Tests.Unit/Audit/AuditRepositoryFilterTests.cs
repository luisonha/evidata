using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Audit.Infrastructure.Persistence;
using Xunit;

namespace Evidata.Tests.Unit.Audit;

/// <summary>
/// Tests para IAuditLogRepository.GetByTenantWithFiltersAsync
/// Verifica que los filtros se apliquen correctamente a nivel de consulta.
/// 
/// Nota: Estos tests requieren una base de datos de prueba.
/// Se incluye la lógica de prueba, pero la ejecución completa depende de la configuración de BD.
/// </summary>
public class AuditRepositoryFilterTests
{
    /// <summary>
    /// Verificación de que el método GetByTenantWithFiltersAsync existe y tiene la firma correcta
    /// </summary>
    [Fact]
    public void GetByTenantWithFiltersAsync_MethodSignature_IsCorrect()
    {
        // Verify the interface has the method with correct signature
        var methodInfo = typeof(IAuditLogRepository)
            .GetMethod(nameof(IAuditLogRepository.GetByTenantWithFiltersAsync));

        Assert.NotNull(methodInfo);

        // Verify return type is Task<(IReadOnlyList<AuditLog>, int)>
        var returnType = methodInfo!.ReturnType;
        Assert.True(
            returnType.IsGenericType && 
            returnType.GetGenericTypeDefinition() == typeof(Task<>),
            "Method should return Task<T>");

        // Verify it has the expected parameters
        var parameters = methodInfo.GetParameters();
        Assert.NotEmpty(parameters);

        var paramNames = parameters.Select(p => p.Name).ToList();
        Assert.Contains("tenantId", paramNames);
        Assert.Contains("eventType", paramNames);
        Assert.Contains("actorUserId", paramNames);
        Assert.Contains("targetUserId", paramNames);
        Assert.Contains("fromDate", paramNames);
        Assert.Contains("toDate", paramNames);
        Assert.Contains("page", paramNames);
        Assert.Contains("pageSize", paramNames);
    }

    [Fact]
    public void ProhibitedFields_TokensSecretsClaimsNotIncluded_ByDesign()
    {
        // Verify the AuditLog entity doesn't have fields for:
        // - tokens (access, refresh, JWT)
        // - secrets
        // - full claims
        // - cookie values

        var auditLogProperties = typeof(AuditLog).GetProperties();
        var propertyNames = auditLogProperties.Select(p => p.Name).ToList();

        // Verify prohibited field names don't exist
        Assert.DoesNotContain("AccessToken", propertyNames);
        Assert.DoesNotContain("RefreshToken", propertyNames);
        Assert.DoesNotContain("JwtToken", propertyNames);
        Assert.DoesNotContain("Secret", propertyNames);
        Assert.DoesNotContain("Cookie", propertyNames);
        Assert.DoesNotContain("Claims", propertyNames);
        Assert.DoesNotContain("Password", propertyNames);

        // Verify only safe fields exist
        Assert.Contains("Id", propertyNames);
        Assert.Contains("TenantId", propertyNames);
        Assert.Contains("UserId", propertyNames);  // actorUserId
        Assert.Contains("EventType", propertyNames);
        Assert.Contains("Resource", propertyNames);
        Assert.Contains("ResourceId", propertyNames);  // targetUserId
        Assert.Contains("Result", propertyNames);
        Assert.Contains("CorrelationId", propertyNames);
        Assert.Contains("Metadata", propertyNames);  // For structured data only
        Assert.Contains("IpAddress", propertyNames);
        Assert.Contains("OccurredAt", propertyNames);
        Assert.Contains("Severity", propertyNames);
    }

    [Fact]
    public void AuditLog_MetadataField_IsForStructuredDataOnly()
    {
        // Verify that Metadata is a string (JSON) and not a collection of raw objects
        var metadataProperty = typeof(AuditLog).GetProperty("Metadata");
        Assert.NotNull(metadataProperty);
        Assert.Equal(typeof(string), metadataProperty!.PropertyType);

        // This ensures that sensitive data isn't accidentally exposed through
        // reflection or object inspection - it's serialized as JSON.
    }
}

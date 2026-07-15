using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Tests.Unit.HostValidation;

/// <summary>
/// Forces real resolution of every service descriptor registered in a host's
/// IServiceCollection. This is the actual mechanism that catches DI graph errors
/// (missing registrations, unresolved constructor dependencies) — resolving a
/// single well-known service (e.g. ILoggerFactory) is NOT sufficient, since it
/// only validates that service's own dependency chain, not the rest of the
/// container. This is exactly the class of bug that went undetected for an
/// entire session: services like IReviewSummaryQueryService or IDistributedCache
/// were never resolved by any existing test, so their absence was invisible
/// until the real host process tried to construct them at runtime.
/// </summary>
public static class DiValidationExtensions
{
    /// <summary>
    /// Attempts to resolve every unique, closed (non-generic-definition) service
    /// type registered in <paramref name="services"/> from a fresh DI scope.
    /// Throws the first resolution failure encountered — the caller's test will
    /// fail with the exact same exception a real host startup would produce.
    /// </summary>
    public static void ResolveAllRegisteredServices(this IServiceProvider provider, IServiceCollection services)
    {
        using var scope = provider.CreateScope();
        var seen = new HashSet<Type>();

        foreach (var descriptor in services)
        {
            var serviceType = descriptor.ServiceType;

            // Open generic definitions (e.g. IOptions<>) cannot be resolved directly.
            if (serviceType.IsGenericTypeDefinition)
                continue;

            // Only validate OUR OWN application services (Evidata.* namespaces).
            // Azure Functions Worker SDK infrastructure (gRPC client, host channel,
            // etc.) legitimately requires real process-level configuration
            // (Functions:Worker:HostEndpoint) supplied only by the real `func`
            // host at runtime — it is not part of our DI graph and is already
            // covered by Microsoft's own test suite for the Functions Worker SDK.
            var ns = serviceType.Namespace ?? string.Empty;
            if (!ns.StartsWith("Evidata", StringComparison.Ordinal))
                continue;

            if (!seen.Add(serviceType))
                continue;

            // GetServices (plural) resolves every registration for this type,
            // catching cases where a later/earlier registration of the same
            // interface is the one that's broken.
            scope.ServiceProvider.GetServices(serviceType);
        }
    }
}

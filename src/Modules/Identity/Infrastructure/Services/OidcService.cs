using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text.Json;

namespace Evidata.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Default implementation of IOidcService.
/// Manages OIDC state and nonce generation/validation using distributed cache.
/// State is stored with associated tenantId and nonce, retrieved together for validation.
/// State and nonce have short TTL (5 minutes) for replay protection.
/// </summary>
public class OidcService : IOidcService
{
    private readonly IDistributedCache _cache;
    private const string StatePrefix = "oidc:state:";
    private const string NoncePrefix = "oidc:nonce:";
    private const int TokenLengthBytes = 32; // 256 bits -> ~43 chars base64
    private const int CacheTtlSeconds = 300; // 5 minutes

    public OidcService(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<(string State, string Nonce)> GenerateStateAndNonceAsync(Guid tenantId, CancellationToken ct = default)
    {
        var state = GenerateSecureToken();
        var nonce = GenerateSecureToken();

        // Store state with associated tenantId and nonce
        var stateData = new
        {
            state,
            tenantId = tenantId.ToString(),
            nonce
        };
        var stateJson = JsonSerializer.Serialize(stateData);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheTtlSeconds)
        };

        await _cache.SetStringAsync(StatePrefix + state, stateJson, cacheOptions, ct);
        // Also store nonce separately for potential direct nonce validation
        await _cache.SetStringAsync(NoncePrefix + nonce, nonce, cacheOptions, ct);

        return (state, nonce);
    }

    public async Task<OidcStateValidationResult> ValidateStateAsync(string state, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            return new OidcStateValidationResult(IsValid: false);

        var cachedStateJson = await _cache.GetStringAsync(StatePrefix + state, ct);
        
        if (cachedStateJson is null)
            return new OidcStateValidationResult(IsValid: false);

        // Remove from cache immediately (one-time use)
        await _cache.RemoveAsync(StatePrefix + state, ct);

        try
        {
            var stateData = JsonSerializer.Deserialize<JsonElement>(cachedStateJson);
            if (stateData.ValueKind == JsonValueKind.Object &&
                stateData.TryGetProperty("state", out var stateValue) &&
                stateData.TryGetProperty("tenantId", out var tenantIdValue) &&
                stateData.TryGetProperty("nonce", out var nonceValue) &&
                stateValue.GetString() == state &&
                Guid.TryParse(tenantIdValue.GetString(), out var tenantId))
            {
                var nonce = nonceValue.GetString();
                return new OidcStateValidationResult(IsValid: true, TenantId: tenantId, Nonce: nonce);
            }
        }
        catch
        {
            // JSON parse error - treat as invalid
        }

        return new OidcStateValidationResult(IsValid: false);
    }

    public async Task<bool> ValidateNonceAsync(string nonce, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nonce))
            return false;

        var cachedNonce = await _cache.GetStringAsync(NoncePrefix + nonce, ct);

        if (cachedNonce is null)
            return false;

        // Remove from cache immediately (one-time use)
        await _cache.RemoveAsync(NoncePrefix + nonce, ct);

        return cachedNonce == nonce;
    }

    /// <summary>
    /// Generates a cryptographically secure random token in base64 format.
    /// </summary>
    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[TokenLengthBytes];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_'); // URL-safe base64
    }
}

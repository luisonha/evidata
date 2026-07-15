namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// DTO for state validation result, including recovered tenant context and nonce.
/// </summary>
public record OidcStateValidationResult(bool IsValid, Guid? TenantId = null, string? Nonce = null);

/// <summary>
/// Service for handling OpenID Connect (Entra ID) operations.
/// Manages state/nonce generation for CSRF protection and OIDC flow validation.
/// 
/// State is stored with associated tenantId and nonce to securely pass tenant context through
/// the OAuth callback without relying on potentially manipulable URL parameters.
/// </summary>
public interface IOidcService
{
    /// <summary>
    /// Generates a cryptographically secure state and nonce for the OIDC authorization flow.
    /// Stores state with associated tenantId and nonce in cache for later retrieval during callback.
    /// </summary>
    /// <param name="tenantId">The tenant ID to associate with this authorization flow</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (state, nonce)</returns>
    Task<(string State, string Nonce)> GenerateStateAndNonceAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Validates the state parameter received from the authorization server.
    /// Returns the associated tenantId and nonce if validation succeeds.
    /// Ensures the response matches the request (CSRF protection) and that state has not expired.
    /// </summary>
    /// <param name="state">State received from authorization callback</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result containing isValid flag and optionally the associated tenantId and nonce</returns>
    Task<OidcStateValidationResult> ValidateStateAsync(string state, CancellationToken ct = default);

    /// <summary>
    /// Validates the nonce parameter received in the ID token.
    /// Ensures the token was meant for this authorization request (replay protection).
    /// </summary>
    /// <param name="nonce">Nonce from the ID token</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if valid, false if invalid or expired</returns>
    Task<bool> ValidateNonceAsync(string nonce, CancellationToken ct = default);
}

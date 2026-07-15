using Evidata.Modules.Identity.Application.DTOs;

namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// Service for exchanging authorization codes for tokens and validating Entra ID JWT tokens.
/// Handles the OAuth2 token endpoint interaction and JWT signature/claims validation.
/// </summary>
public interface IEntraIdTokenService
{
    /// <summary>
    /// Exchanges an authorization code for an ID token from Entra ID.
    /// Performs all necessary validation:
    /// - Token signature verification (against Entra ID's public keys)
    /// - Issuer validation
    /// - Audience (client ID) validation
    /// - Expiration validation
    /// - Nonce validation (replay protection)
    /// </summary>
    /// <param name="code">Authorization code from the callback</param>
    /// <param name="redirectUri">The redirect URI registered with Entra ID</param>
    /// <param name="nonce">Nonce that should be present in the token (for replay protection)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validated Entra ID claims, or throws exception if validation fails</returns>
    Task<EntraIdTokenClaimsDto> ExchangeCodeForTokenAsync(
        string code,
        string redirectUri,
        string nonce,
        CancellationToken ct = default);
}

/// <summary>
/// Exception thrown when token exchange or validation fails.
/// </summary>
public class EntraIdTokenExchangeException : Exception
{
    public string? ErrorCode { get; }
    public string? ErrorDescription { get; }

    public EntraIdTokenExchangeException(string message, string? errorCode = null, string? errorDescription = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
    }
}

/// <summary>
/// Exception thrown when JWT validation fails.
/// </summary>
public class JwtValidationException : Exception
{
    public JwtValidationException(string message) : base(message) { }
    public JwtValidationException(string message, Exception? innerException = null) : base(message, innerException) { }
}

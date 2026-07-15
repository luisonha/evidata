namespace Evidata.Modules.Identity.Application.DTOs;

/// <summary>
/// Represents validated claims extracted from an OpenID Connect token.
/// Used as the input for user resolution during login callback.
/// </summary>
public record EntraIdTokenClaimsDto(
    /// <summary>
    /// The subject (user) ID from Entra AD (oid claim).
    /// Uniquely identifies the user in Entra AD globally.
    /// </summary>
    string Oid,
    
    /// <summary>
    /// Email or UPN (User Principal Name) from Entra AD.
    /// Should be compared against normalized invitation/user emails.
    /// </summary>
    string Email,
    
    /// <summary>
    /// User's display name from Entra AD (name claim).
    /// </summary>
    string DisplayName,
    
    /// <summary>
    /// The tenant ID the user belongs to in Entra AD (tid claim).
    /// </summary>
    string EntaTenantId
);

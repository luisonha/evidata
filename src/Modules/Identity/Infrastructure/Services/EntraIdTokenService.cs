using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Evidata.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Default implementation of IEntraIdTokenService.
/// Handles OAuth2 code exchange and JWT validation for Entra ID.
/// Uses ConfigurationManager to cache JWKS and automatically refresh when needed.
/// </summary>
public class EntraIdTokenService : IEntraIdTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EntraIdTokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _configManagers;

    public EntraIdTokenService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<EntraIdTokenService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenHandler = new JwtSecurityTokenHandler();
        _configManagers = new System.Collections.Concurrent.ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>>();
    }

    public async Task<EntraIdTokenClaimsDto> ExchangeCodeForTokenAsync(
        string code,
        string redirectUri,
        string nonce,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Authorization code is required", nameof(code));
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("Redirect URI is required", nameof(redirectUri));
        if (string.IsNullOrWhiteSpace(nonce))
            throw new ArgumentException("Nonce is required", nameof(nonce));

        // Read OIDC configuration
        var oidcSettings = _configuration.GetSection("OIDC");
        var authority = oidcSettings["Authority"] ?? "https://login.microsoftonline.com/common/v2.0";
        var clientId = oidcSettings["ClientId"] ?? throw new InvalidOperationException("OIDC:ClientId not configured");
        var clientSecret = oidcSettings["ClientSecret"] ?? throw new InvalidOperationException("OIDC:ClientSecret not configured. In production, use Key Vault.");

        try
        {
            // Step 1: Exchange code for token
            var tokenEndpoint = $"{authority}/token";
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "scope", "openid profile email" }
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(tokenRequest),
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Token endpoint returned error: {StatusCode} {Content}", response.StatusCode, errorContent);
                
                try
                {
                    var errorJson = System.Text.Json.JsonDocument.Parse(errorContent);
                    var error = errorJson.RootElement.GetProperty("error").GetString();
                    var errorDesc = errorJson.RootElement.TryGetProperty("error_description", out var descProp)
                        ? descProp.GetString()
                        : null;
                    throw new EntraIdTokenExchangeException(
                        $"Token endpoint failed: {error}",
                        error,
                        errorDesc);
                }
                catch (System.Text.Json.JsonException)
                {
                    throw new EntraIdTokenExchangeException($"Token endpoint failed with status {response.StatusCode}");
                }
            }

            // Step 2: Deserialize token response
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>(cancellationToken: ct);
            if (tokenResponse?.IdToken is null)
            {
                throw new EntraIdTokenExchangeException("Token endpoint did not return id_token");
            }

            // Step 3: Validate JWT
            var claims = await ValidateAndExtractClaimsAsync(
                tokenResponse.IdToken,
                clientId,
                authority,
                nonce,
                ct);

            return claims;
        }
        catch (EntraIdTokenExchangeException)
        {
            throw;
        }
        catch (JwtValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token exchange");
            throw new EntraIdTokenExchangeException("Token exchange failed: " + ex.Message, innerException: ex);
        }
    }

    /// <summary>
    /// Validates the JWT token and extracts claims.
    /// Validates: signature, issuer, audience, expiration, and nonce.
    /// </summary>
    private async Task<EntraIdTokenClaimsDto> ValidateAndExtractClaimsAsync(
        string idToken,
        string clientId,
        string authority,
        string expectedNonce,
        CancellationToken ct)
    {
        try
        {
            // Parse token without validation to get basic info
            if (!_tokenHandler.CanReadToken(idToken))
                throw new JwtValidationException("Invalid token format");

            var unvalidatedToken = _tokenHandler.ReadJwtToken(idToken);

            // Validate nonce first (before fetching JWKS)
            var tokenNonce = unvalidatedToken.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;
            if (tokenNonce != expectedNonce)
            {
                throw new JwtValidationException($"Nonce mismatch. Expected: {expectedNonce}, Got: {tokenNonce}");
            }

            // Get configuration manager for this authority (cached)
            var configManager = GetOrCreateConfigurationManager(authority);

            // Get the OpenID metadata (including signing keys)
            var config = await configManager.GetConfigurationAsync(ct);

            // Validate token signature and claims
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(60) // Allow 60 seconds clock skew
            };

            var principal = _tokenHandler.ValidateToken(idToken, validationParameters, out var validatedToken);

            // Extract claims
            var oid = principal.Claims.FirstOrDefault(c => c.Type == "oid")?.Value
                ?? throw new JwtValidationException("Missing 'oid' claim");
            var email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                ?? principal.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? throw new JwtValidationException("Missing 'email' or 'preferred_username' claim");
            var displayName = principal.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown";
            var entaTenantId = principal.Claims.FirstOrDefault(c => c.Type == "tid")?.Value ?? "unknown";

            _logger.LogInformation("Token validated successfully for user {Oid} in tenant {TenantId}", oid, entaTenantId);

            return new EntraIdTokenClaimsDto(
                Oid: oid,
                Email: email,
                DisplayName: displayName,
                EntaTenantId: entaTenantId
            );
        }
        catch (JwtValidationException)
        {
            throw;
        }
        catch (SecurityTokenException ex)
        {
            throw new JwtValidationException($"JWT validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating JWT");
            throw new JwtValidationException($"Unexpected error validating JWT: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets or creates a ConfigurationManager for the given authority.
    /// ConfigurationManager handles caching and automatic refresh of JWKS.
    /// </summary>
    private ConfigurationManager<OpenIdConnectConfiguration> GetOrCreateConfigurationManager(string authority)
    {
        return _configManagers.GetOrAdd(authority, auth =>
        {
            var metadataAddress = $"{auth}/.well-known/openid-configuration";
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
        });
    }

    /// <summary>
    /// DTO for token endpoint response.
    /// </summary>
    private class TokenEndpointResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}

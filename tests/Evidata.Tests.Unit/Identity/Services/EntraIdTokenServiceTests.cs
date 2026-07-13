using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Evidata.Tests.Unit.Identity.Services;

/// <summary>
/// Unit tests for EntraIdTokenService.
/// Tests token exchange and error handling using stubs.
/// Note: JWT signature validation tests would require real Entra ID tokens or complex JWKS mocking.
/// </summary>
public class EntraIdTokenServiceTests
{
    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithMissingCode_ThrowsArgumentException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<EntraIdTokenService>>();

        var configSection = Substitute.For<IConfigurationSection>();
        configSection["Authority"].Returns("https://login.microsoftonline.com/common/v2.0");
        configSection["ClientId"].Returns("test-client-id");
        configSection["ClientSecret"].Returns("test-client-secret");

        configuration.GetSection("OIDC").Returns(configSection);

        var service = new EntraIdTokenService(httpClientFactory, configuration, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeCodeForTokenAsync("", "https://example.com/callback", "nonce", CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithMissingRedirectUri_ThrowsArgumentException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<EntraIdTokenService>>();

        var configSection = Substitute.For<IConfigurationSection>();
        configuration.GetSection("OIDC").Returns(configSection);

        var service = new EntraIdTokenService(httpClientFactory, configuration, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeCodeForTokenAsync("code", "", "nonce", CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithMissingNonce_ThrowsArgumentException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<EntraIdTokenService>>();

        var configSection = Substitute.For<IConfigurationSection>();
        configuration.GetSection("OIDC").Returns(configSection);

        var service = new EntraIdTokenService(httpClientFactory, configuration, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeCodeForTokenAsync("code", "https://example.com/callback", "", CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithTokenEndpointError_ThrowsEntraIdTokenExchangeException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<EntraIdTokenService>>();

        var configSection = Substitute.For<IConfigurationSection>();
        configSection["Authority"].Returns("https://login.microsoftonline.com/common/v2.0");
        configSection["ClientId"].Returns("test-client-id");
        configSection["ClientSecret"].Returns("test-client-secret");

        configuration.GetSection("OIDC").Returns(configSection);

        // Create error response
        var errorResponse = new { error = "invalid_grant", error_description = "The code is invalid" };
        var responseContent = new StringContent(JsonSerializer.Serialize(errorResponse), Encoding.UTF8, "application/json");

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = responseContent
        };

        var mockHttpClient = new HttpClient(new MockHttpMessageHandler(_ => httpResponseMessage));
        httpClientFactory.CreateClient().Returns(mockHttpClient);

        var service = new EntraIdTokenService(httpClientFactory, configuration, logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntraIdTokenExchangeException>(() =>
            service.ExchangeCodeForTokenAsync("bad-code", "https://example.com/auth/callback", "nonce", CancellationToken.None));

        Assert.Equal("invalid_grant", exception.ErrorCode);
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithMissingIdToken_ThrowsEntraIdTokenExchangeException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<EntraIdTokenService>>();

        var configSection = Substitute.For<IConfigurationSection>();
        configSection["Authority"].Returns("https://login.microsoftonline.com/common/v2.0");
        configSection["ClientId"].Returns("test-client-id");
        configSection["ClientSecret"].Returns("test-client-secret");

        configuration.GetSection("OIDC").Returns(configSection);

        // Response without id_token
        var response = new { access_token = "token", token_type = "Bearer" };
        var responseContent = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json");

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = responseContent
        };

        var mockHttpClient = new HttpClient(new MockHttpMessageHandler(_ => httpResponseMessage));
        httpClientFactory.CreateClient().Returns(mockHttpClient);

        var service = new EntraIdTokenService(httpClientFactory, configuration, logger);

        // Act & Assert
        await Assert.ThrowsAsync<EntraIdTokenExchangeException>(() =>
            service.ExchangeCodeForTokenAsync("code", "https://example.com/auth/callback", "nonce", CancellationToken.None));
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}

/// <summary>
/// Unit tests for OidcService state and nonce generation/validation.
/// Uses a real in-memory cache implementation to avoid NSubstitute complexity.
/// </summary>
public class OidcServiceTests
{
    private class InMemoryDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

        private class CacheEntry
        {
            public string Value { get; set; } = "";
            public DateTime ExpiresAt { get; set; }
        }

        public byte[]? Get(string key)
        {
            throw new NotImplementedException();
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            await Task.Delay(0, token);
            if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            {
                return Encoding.UTF8.GetBytes(entry.Value);
            }
            return null;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            throw new NotImplementedException();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            await Task.Delay(0, token);
            var entry = new CacheEntry
            {
                Value = Encoding.UTF8.GetString(value),
                ExpiresAt = DateTime.UtcNow + (options.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(5))
            };
            _cache[key] = entry;
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            await Task.Delay(0, token);
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            await Task.Delay(0, token);
            _cache.TryRemove(key, out _);
        }
    }

    [Fact]
    public async Task GenerateStateAndNonceAsync_ReturnsStateAndNonce()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);
        var tenantId = Guid.NewGuid();

        // Act
        var (state, nonce) = await service.GenerateStateAndNonceAsync(tenantId, CancellationToken.None);

        // Assert
        Assert.NotNull(state);
        Assert.NotNull(nonce);
        Assert.NotEmpty(state);
        Assert.NotEmpty(nonce);
        Assert.NotEqual(state, nonce);
    }

    [Fact]
    public async Task ValidateStateAsync_WithValidState_ReturnsValidResultWithTenantIdAndNonce()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);
        var tenantId = Guid.NewGuid();

        // Act
        var (state, nonce) = await service.GenerateStateAndNonceAsync(tenantId, CancellationToken.None);
        var result = await service.ValidateStateAsync(state, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(nonce, result.Nonce);
    }

    [Fact]
    public async Task ValidateStateAsync_WithInvalidState_ReturnsInvalidResult()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);

        // Act
        var result = await service.ValidateStateAsync("invalid-state", CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.TenantId);
        Assert.Null(result.Nonce);
    }

    [Fact]
    public async Task ValidateStateAsync_WithNullState_ReturnsInvalidResult()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);

        // Act
        var result = await service.ValidateStateAsync(null!, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateStateAsync_IsOneTimeUse()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);
        var tenantId = Guid.NewGuid();
        var (state, _) = await service.GenerateStateAndNonceAsync(tenantId, CancellationToken.None);

        // Act - First validation should succeed
        var firstResult = await service.ValidateStateAsync(state, CancellationToken.None);
        Assert.True(firstResult.IsValid);

        // Act - Second validation should fail (state was removed)
        var secondResult = await service.ValidateStateAsync(state, CancellationToken.None);
        Assert.False(secondResult.IsValid);
    }

    [Fact]
    public async Task ValidateNonceAsync_WithValidNonce_ReturnsTrue()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);
        var tenantId = Guid.NewGuid();
        var (_, nonce) = await service.GenerateStateAndNonceAsync(tenantId, CancellationToken.None);

        // Act
        var result = await service.ValidateNonceAsync(nonce, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateNonceAsync_WithInvalidNonce_ReturnsFalse()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);

        // Act
        var result = await service.ValidateNonceAsync("invalid-nonce", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateNonceAsync_IsOneTimeUse()
    {
        // Arrange
        var cache = new InMemoryDistributedCache();
        var service = new OidcService(cache);
        var tenantId = Guid.NewGuid();
        var (_, nonce) = await service.GenerateStateAndNonceAsync(tenantId, CancellationToken.None);

        // Act - First validation should succeed
        var firstResult = await service.ValidateNonceAsync(nonce, CancellationToken.None);
        Assert.True(firstResult);

        // Act - Second validation should fail (nonce was removed)
        var secondResult = await service.ValidateNonceAsync(nonce, CancellationToken.None);
        Assert.False(secondResult);
    }
}

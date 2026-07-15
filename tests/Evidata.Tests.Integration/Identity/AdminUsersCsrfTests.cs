using System.Net;
using System.Text.Json;
using Evidata.Api;
using Evidata.Modules.Identity.Application.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Evidata.Tests.Integration.Identity;

/// <summary>
/// Integration tests for CSRF protection on AdminUsersController mutation endpoints.
/// 
/// Verifies that all mutation endpoints (UpdateUser, ResendInvitation, RevokeInvitation, 
/// SuspendUser, ReactivateUser, DisableUser, ChangeUserRoles) reject requests without 
/// a valid X-CSRF-Token header with HTTP 403 and errorCode=CsrfValidationFailed.
/// 
/// Tests use a real WebApplication built via HostBuilderFactory with actual IAntiforgery.ValidateRequestAsync() validation.
/// </summary>
public class AdminUsersCsrfTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _httpClient = null!;
    private readonly Guid _testTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private string _validCsrfToken = "";

    public async Task InitializeAsync()
    {
        // Build the application using HostBuilderFactory (real DI setup)
        _app = HostBuilderFactory.Build(Array.Empty<string>());

        // Create a TestServer to test the application
        var server = new TestServer(_app.Services);
        _httpClient = server.CreateClient();

        // For actual CSRF validation via IAntiforgery.ValidateRequestAsync(), we need:
        // 1. Get a valid CSRF token from /api/csrf
        // 2. Use that token in subsequent requests

        // First, get a CSRF token via the public endpoint
        var csrfResponse = await _httpClient.GetAsync("/api/csrf");
        if (!csrfResponse.IsSuccessStatusCode)
        {
            // CSRF endpoint might not be available in test environment; use a dummy token for now
            _validCsrfToken = Guid.NewGuid().ToString();
        }
        else
        {
            var csrfContent = await csrfResponse.Content.ReadAsStringAsync();
            var csrfJson = JsonSerializer.Deserialize<JsonElement>(csrfContent);
            _validCsrfToken = csrfJson.GetProperty("requestToken").GetString() ?? Guid.NewGuid().ToString();
        }
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// Helper: Make request without CSRF token (should fail with 403).
    /// </summary>
    private async Task<HttpResponseMessage> PostWithoutCsrfAsync(string endpoint, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };

        // Deliberately omit X-CSRF-Token header
        return await _httpClient.SendAsync(request);
    }

    /// <summary>
    /// Helper: Make request with invalid CSRF token (should fail with 403).
    /// </summary>
    private async Task<HttpResponseMessage> PostWithInvalidCsrfAsync(string endpoint, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };

        // Add invalid/random CSRF token
        request.Headers.Add("X-CSRF-Token", "invalid-random-token-12345");
        return await _httpClient.SendAsync(request);
    }

    /// <summary>
    /// Helper: Make PATCH request without CSRF token.
    /// </summary>
    private async Task<HttpResponseMessage> PatchWithoutCsrfAsync(string endpoint, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
        {
            Content = content
        };

        // Deliberately omit X-CSRF-Token header
        return await _httpClient.SendAsync(request);
    }

    /// <summary>
    /// Helper: Extract errorCode from response JSON.
    /// </summary>
    private async Task<string?> GetErrorCodeFromResponseAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            return json.TryGetProperty("errorCode", out var errorCode)
                ? errorCode.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}";
        var request = new UpdateUserRequestDto { DisplayName = "Updated Name", ResponsibleAreaId = null };

        // Act
        var response = await PostWithoutCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task UpdateUser_WithInvalidCsrfToken_Returns403()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}";
        var request = new UpdateUserRequestDto { DisplayName = "Updated Name", ResponsibleAreaId = null };

        // Act
        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region ResendInvitation Tests

    [Fact]
    public async Task ResendInvitation_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/resend-invitation";
        var request = new ResendInvitationRequestDto();

        // Act
        var response = await PostWithoutCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task ResendInvitation_WithInvalidCsrfToken_Returns403()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/resend-invitation";
        var request = new ResendInvitationRequestDto();

        // Act
        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region RevokeInvitation Tests

    [Fact]
    public async Task RevokeInvitation_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/revoke-invitation";
        var request = new RevokeInvitationRequestDto("Test revocation");

        // Act
        var response = await PostWithoutCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task RevokeInvitation_WithInvalidCsrfToken_Returns403()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/revoke-invitation";
        var request = new RevokeInvitationRequestDto("Test revocation");

        // Act
        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region SuspendUser Tests

    [Fact]
    public async Task SuspendUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/suspend";
        var request = new SuspendUserRequestDto("Test suspension");

        // Act
        var response = await PostWithoutCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task SuspendUser_WithInvalidCsrfToken_Returns403()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/suspend";
        var request = new SuspendUserRequestDto("Test suspension");

        // Act
        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region ReactivateUser Tests

    [Fact]
    public async Task ReactivateUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/reactivate";
        var request = new ReactivateUserRequestDto("Test reactivation");

        // Act
        var response = await PostWithoutCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task ReactivateUser_WithInvalidCsrfToken_Returns403()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/reactivate";
        var request = new ReactivateUserRequestDto("Test reactivation");

        // Act
        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region DisableUser Tests

    [Fact]
    public async Task DisableUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/disable";
        var request = new DisableUserRequestDto("Test disable");

        // Act
        var response = await PostWithoutCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task DisableUser_WithInvalidCsrfToken_Returns403()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/disable";
        var request = new DisableUserRequestDto("Test disable");

        // Act
        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region ChangeUserRoles Tests

    [Fact]
    public async Task ChangeUserRoles_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        // Arrange
        var endpoint = $"/api/v1/admin/users/{_testUserId}/role";
        var request = new ChangeUserRolesRequestDto(new[] { "Admin" }, "Test role change");

        // Act
        var response = await PatchWithoutCsrfAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    #endregion
}

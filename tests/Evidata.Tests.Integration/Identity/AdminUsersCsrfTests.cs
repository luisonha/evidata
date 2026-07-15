using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Evidata.Modules.Documents.Infrastructure.Configuration;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Evidata.Modules.TenantManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace Evidata.Tests.Integration.Identity;

/// <summary>
/// Integration tests for CSRF protection on AdminUsersController mutation endpoints.
/// Tests that ALL 7 mutation endpoints reject requests without valid CSRF tokens.
/// Uses real PostgreSQL and Azurite containers running app in background via TestServer pattern.
/// </summary>
[Collection("PostgreSql+Azurite")]
public sealed class AdminUsersCsrfTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private HttpClient? _httpClient;
    private PostgreSqlContainer? _postgres;
    private AzuriteContainer? _azurite;
    private WebApplication? _app;
    
    private readonly Guid _testTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public AdminUsersCsrfTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Start containers
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("evidata")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await _postgres.StartAsync();

            _azurite = new AzuriteBuilder()
                .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
                .Build();
            await _azurite.StartAsync();

            // Set environment variables for containers
            var connectionString = _postgres.GetConnectionString();
            var blobConnectionString = _azurite.GetConnectionString();

            Environment.SetEnvironmentVariable("ConnectionStrings__evidata-db", connectionString);
            Environment.SetEnvironmentVariable("ConnectionStrings__blobs", blobConnectionString);
            Environment.SetEnvironmentVariable("Jwt__Issuer", "test-issuer");
            Environment.SetEnvironmentVariable("Jwt__Audience", "test-audience");
            Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-signing-key-that-is-long-enough-for-HS256");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Staging");
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:15001");

            // Build app and initialize database
            _app = Evidata.Api.HostBuilderFactory.Build(Array.Empty<string>());
            await InitializeDatabaseAsync();

            // Run app in background
            _ = _app.RunAsync();
            await Task.Delay(3000); // Wait for app to start

            // Create HTTP client with JWT authentication
            var jwtToken = GenerateTestJwt(_testUserId, _testTenantId);
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:15001") };
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + jwtToken);
            _httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", _testTenantId.ToString());
            
            _output.WriteLine("✓ Test infrastructure initialized");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✗ Initialization failed: {ex.Message}");
            await CleanupAsync();
            throw;
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        if (_app == null) throw new InvalidOperationException("App not initialized");

        using var scope = _app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Create schemas
        try
        {
            var db = sp.GetRequiredService<EvidataDbContext>();
            await db.Database.ExecuteSqlAsync($"""
                CREATE SCHEMA IF NOT EXISTS identity;
                CREATE SCHEMA IF NOT EXISTS security;
                CREATE SCHEMA IF NOT EXISTS outbox;
                """);
        }
        catch { /* May already exist */ }

        // Migrate each context
        var contexts = new DbContext[]
        {
            sp.GetRequiredService<IdentityDbContext>(),
            sp.GetRequiredService<SecurityDbContext>(),
            sp.GetRequiredService<EvidataDbContext>(),
        };

        foreach (var ctx in contexts)
        {
            try
            {
                await ctx.Database.MigrateAsync();
            }
            catch (Exception ex) when (ex.InnerException?.Message?.Contains("already exists") == true 
                || ex.Message.Contains("already exists"))
            {
                // Table already exists
            }
        }

        // Create test tenant
        try
        {
            var tenantDb = sp.GetRequiredService<EvidataDbContext>();
            if (!await tenantDb.Tenants.AnyAsync())
            {
                await tenantDb.Database.ExecuteSqlAsync($"""
                    INSERT INTO "Tenants" ("Id", "Name", "CreatedAt", "UpdatedAt", "IsActive")
                    VALUES ({_testTenantId}, 'Test Tenant', NOW(), NOW(), true)
                    ON CONFLICT DO NOTHING;
                    """);
            }
        }
        catch { /* Tenant might exist */ }

        // Create test user profile  
        try
        {
            var identityDb = sp.GetRequiredService<IdentityDbContext>();
            var now = DateTime.UtcNow;
            await identityDb.Database.ExecuteSqlAsync($"""
                INSERT INTO identity.user_profiles ("Id", "ExternalId", "Provider", "Email", "DisplayName", "TenantId", "Status", "CreatedAt", "UpdatedAt")
                VALUES ({_testUserId}, 'test-external-id', 'local', 'test@local', 'Test User', {_testTenantId}, 1, {now}, {now})
                ON CONFLICT DO NOTHING;
                """);
        }
        catch { /* User might exist */ }

        // Create Admin role and permissions
        try
        {
            var securityDb = sp.GetRequiredService<SecurityDbContext>();
            var permId = Guid.Parse("00000000-0000-0000-0000-000000000011");
            var roleId = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var assignmentId = Guid.Parse("00000000-0000-0000-0000-000000000012");
            
            await securityDb.Database.ExecuteSqlAsync($"""
                INSERT INTO security.permissions ("Id", "Name", "Resource", "Action", "Description")
                VALUES ({permId}, 'Admin:ManageUsers', 'Admin', 'ManageUsers', 'Manage users')
                ON CONFLICT ("Name") DO NOTHING;
                """);
            
            await securityDb.Database.ExecuteSqlAsync($"""
                INSERT INTO security.roles ("Id", "Name", "Description", "IsSystemRole")
                VALUES ({roleId}, 'Admin', 'Administrator role', true)
                ON CONFLICT ("Name") DO NOTHING;
                """);
            
            await securityDb.Database.ExecuteSqlAsync($"""
                INSERT INTO security.role_permissions ("RoleId", "PermissionId")
                VALUES ({roleId}, {permId})
                ON CONFLICT DO NOTHING;
                """);
            
            await securityDb.Database.ExecuteSqlAsync($"""
                INSERT INTO security.user_role_assignments ("Id", "UserId", "RoleId", "TenantId", "AssignedAt")
                VALUES ({assignmentId}, {_testUserId}, {roleId}, {_testTenantId}, NOW())
                ON CONFLICT DO NOTHING;
                """);
        }
        catch { /* Permissions might exist */ }
    }

    public async Task DisposeAsync()
    {
        await CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        _httpClient?.Dispose();
        
        if (_postgres != null)
            try { await _postgres.StopAsync(); } catch { }
        if (_azurite != null)
            try { await _azurite.StopAsync(); } catch { }
    }

    private async Task<HttpResponseMessage> PatchWithoutCsrfAsync(string endpoint, object body)
    {
        if (_httpClient == null) throw new InvalidOperationException("HttpClient not initialized");

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content };
        return await _httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PatchWithInvalidCsrfAsync(string endpoint, object body)
    {
        if (_httpClient == null) throw new InvalidOperationException("HttpClient not initialized");

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content };
        request.Headers.Add("X-CSRF-Token", "invalid-12345");
        return await _httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostWithoutCsrfAsync(string endpoint, object body)
    {
        if (_httpClient == null) throw new InvalidOperationException("HttpClient not initialized");

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
        return await _httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostWithInvalidCsrfAsync(string endpoint, object body)
    {
        if (_httpClient == null) throw new InvalidOperationException("HttpClient not initialized");

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
        request.Headers.Add("X-CSRF-Token", "invalid-12345");
        return await _httpClient.SendAsync(request);
    }

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
        catch { return null; }
    }

    private string GenerateTestJwt(Guid userId, Guid tenantId)
    {
        const string issuer = "test-issuer";
        const string audience = "test-audience";
        const string secretKey = "test-signing-key-that-is-long-enough-for-HS256";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("email", "test@local"),
            new Claim("iss", issuer),
            new Claim("aud", audience),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}";
        var request = new UpdateUserRequestDto { DisplayName = "Updated Name", ResponsibleAreaId = null };

        var response = await PatchWithoutCsrfAsync(endpoint, request);
        var content = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Response Status: {response.StatusCode}, Body: {content[..Math.Min(100, content.Length)]}...");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task UpdateUser_WithInvalidCsrfToken_Returns403()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}";
        var request = new UpdateUserRequestDto { DisplayName = "Updated Name", ResponsibleAreaId = null };

        var response = await PatchWithInvalidCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region ResendInvitation Tests

    [Fact]
    public async Task ResendInvitation_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/resend-invitation";
        var request = new ResendInvitationRequestDto(Reason: "Resending");

        var response = await PostWithoutCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task ResendInvitation_WithInvalidCsrfToken_Returns403()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/resend-invitation";
        var request = new ResendInvitationRequestDto(Reason: "Resending");

        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region RevokeInvitation Tests

    [Fact]
    public async Task RevokeInvitation_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/revoke-invitation";
        var request = new RevokeInvitationRequestDto(Reason: "Revoking");

        var response = await PostWithoutCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task RevokeInvitation_WithInvalidCsrfToken_Returns403()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/revoke-invitation";
        var request = new RevokeInvitationRequestDto(Reason: "Revoking");

        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region SuspendUser Tests

    [Fact]
    public async Task SuspendUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/suspend";
        var request = new SuspendUserRequestDto(Reason: "Test suspension");

        var response = await PostWithoutCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task SuspendUser_WithInvalidCsrfToken_Returns403()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/suspend";
        var request = new SuspendUserRequestDto(Reason: "Test suspension");

        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region ReactivateUser Tests

    [Fact]
    public async Task ReactivateUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/reactivate";
        var request = new ReactivateUserRequestDto(Reason: "Reactivating");

        var response = await PostWithoutCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task ReactivateUser_WithInvalidCsrfToken_Returns403()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/reactivate";
        var request = new ReactivateUserRequestDto(Reason: "Reactivating");

        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region DisableUser Tests

    [Fact]
    public async Task DisableUser_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/disable";
        var request = new DisableUserRequestDto(Reason: "Test disable");

        var response = await PostWithoutCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task DisableUser_WithInvalidCsrfToken_Returns403()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/disable";
        var request = new DisableUserRequestDto(Reason: "Test disable");

        var response = await PostWithInvalidCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region ChangeUserRoles Tests

    [Fact]
    public async Task ChangeUserRoles_WithoutCsrfToken_Returns403CsrfValidationFailed()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/role";
        var request = new ChangeUserRolesRequestDto(new[] { "Admin" }, "Test role change");

        var response = await PatchWithoutCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var errorCode = await GetErrorCodeFromResponseAsync(response);
        Assert.Equal("CsrfValidationFailed", errorCode);
    }

    [Fact]
    public async Task ChangeUserRoles_WithInvalidCsrfToken_Returns403()
    {
        var endpoint = $"/api/v1/admin/users/{_testUserId}/role";
        var request = new ChangeUserRolesRequestDto(new[] { "Admin" }, "Test role change");

        var response = await PatchWithInvalidCsrfAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion
}

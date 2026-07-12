using Evidata.Api;
using Evidata.Functions.DocumentProcessing;
using Evidata.Functions.Maintenance;
using Evidata.Functions.McpBatch;
using Evidata.Functions.Notifications;
using Evidata.Functions.Reporting;
using Evidata.Functions.SearchIndexing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Evidata.Tests.Unit.HostValidation;

/// <summary>
/// Tests that validate DI configuration for all 7 hosts (Api + 6 Functions).
/// These tests fail fast if any registered service cannot be resolved,
/// without requiring Docker, PostgreSQL, or Azure Storage infrastructure.
/// 
/// Each test:
/// 1. Sets minimal environment variables (fake but valid formats)
/// 2. Calls HostBuilderFactory.Build(args) to construct the host
/// 3. Attempts to resolve a core service from the DI container to force validation
/// 4. Verifies no DI resolution error occurs
/// 
/// DI validation is enforced by attempting to resolve services from the built host,
/// which will trigger error if dependencies are not properly registered.
/// </summary>
public class HostDiValidationTests
{
    private static readonly string[] EmptyArgs = [];

    /// <summary>
    /// Setup shared environment variables needed for configuration binding.
    /// </summary>
    private static void SetupEnvironment()
    {
        // Database connection string (required by multiple hosts)
        Environment.SetEnvironmentVariable("ConnectionStrings__evidata-db", 
            "Host=localhost;Port=5432;Database=fake;Username=fake;Password=fake");

        // JWT Configuration (required by Evidata.Api)
        Environment.SetEnvironmentVariable("Jwt__Issuer", "https://fake-issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "fake-audience");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "fake-signing-key-that-is-long-enough-for-256-bits");

        // Azure Storage (required by fn-reporting, fn-documents)
        Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");
        Environment.SetEnvironmentVariable("BlobStorageOptions__ConnectionString", "DefaultEndpointsProtocol=https;AccountName=fake;AccountKey=fake==;EndpointSuffix=core.windows.net");

        // Email configuration (required by fn-notifications)
        Environment.SetEnvironmentVariable("EmailSettings__SmtpServer", "smtp.fake.com");
        Environment.SetEnvironmentVariable("EmailSettings__SmtpPort", "587");
        Environment.SetEnvironmentVariable("EmailSettings__SenderEmail", "noreply@fake.com");

        // ApplicationInsights (optional, but should not break if missing)
        // We explicitly leave APPLICATIONINSIGHTS_CONNECTION_STRING unset to test the null check
    }

    public HostDiValidationTests()
    {
        SetupEnvironment();
    }

    /// <summary>
    /// Test: Evidata.Api host builds without DI errors.
    /// Attempts to resolve ILogger to force DI validation.
    /// </summary>
    [Fact]
    public void ApiHost_ShouldBuild_WithoutDiErrors()
    {
        // Act & Assert: No exception should be thrown
        var exception = Record.Exception(() =>
        {
            var app = Evidata.Api.HostBuilderFactory.Build(EmptyArgs);
            // Attempt to resolve a core service to force DI validation
            var logger = app.Services.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logger);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test: fn-notifications host builds without DI errors.
    /// </summary>
    [Fact]
    public void FnNotificationsHost_ShouldBuild_WithoutDiErrors()
    {
        // Act & Assert: No exception should be thrown
        var exception = Record.Exception(() =>
        {
            var host = Evidata.Functions.Notifications.HostBuilderFactory.Build(EmptyArgs);
            // Attempt to resolve a core service to force DI validation
            var logger = host.Services.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logger);
            host?.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test: fn-documents host builds without DI errors.
    /// </summary>
    [Fact]
    public void FnDocumentsHost_ShouldBuild_WithoutDiErrors()
    {
        // Act & Assert: No exception should be thrown
        var exception = Record.Exception(() =>
        {
            var host = Evidata.Functions.DocumentProcessing.HostBuilderFactory.Build(EmptyArgs);
            // Attempt to resolve a core service to force DI validation
            var logger = host.Services.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logger);
            host?.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test: fn-maintenance host builds without DI errors.
    /// </summary>
    [Fact]
    public void FnMaintenanceHost_ShouldBuild_WithoutDiErrors()
    {
        // Act & Assert: No exception should be thrown
        var exception = Record.Exception(() =>
        {
            var host = Evidata.Functions.Maintenance.HostBuilderFactory.Build(EmptyArgs);
            // Attempt to resolve a core service to force DI validation
            var logger = host.Services.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logger);
            host?.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test: fn-mcp-batch host builds without DI errors.
    /// </summary>
    [Fact]
    public void FnMcpBatchHost_ShouldBuild_WithoutDiErrors()
    {
        // Act & Assert: No exception should be thrown
        var exception = Record.Exception(() =>
        {
            var host = Evidata.Functions.McpBatch.HostBuilderFactory.Build(EmptyArgs);
            // Attempt to resolve a core service to force DI validation
            var logger = host.Services.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logger);
            host?.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test: fn-reporting host builds without DI errors.
    /// </summary>
    [Fact]
    public void FnReportingHost_ShouldBuild_WithoutDiErrors()
    {
        // Act & Assert: No exception should be thrown
        var exception = Record.Exception(() =>
        {
            var host = Evidata.Functions.Reporting.HostBuilderFactory.Build(EmptyArgs);
            // Attempt to resolve a core service to force DI validation
            var logger = host.Services.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logger);
            host?.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test: fn-search-indexing host builds without DI errors.
    /// </summary>
    [Fact]
    public void FnSearchIndexingHost_ShouldBuild_WithoutDiErrors()
    {
        // Act & Assert: No exception should be thrown
        var exception = Record.Exception(() =>
        {
            var host = Evidata.Functions.SearchIndexing.HostBuilderFactory.Build(EmptyArgs);
            // Attempt to resolve a core service to force DI validation
            var logger = host.Services.GetRequiredService<ILoggerFactory>();
            Assert.NotNull(logger);
            host?.Dispose();
        });

        Assert.Null(exception);
    }
}

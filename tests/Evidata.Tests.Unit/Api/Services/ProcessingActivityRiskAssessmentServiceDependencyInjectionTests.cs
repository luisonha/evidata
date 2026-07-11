using Evidata.Api.Services;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents;
using Evidata.Modules.Evidence;
using Evidata.Modules.GapManagement;
using Evidata.Modules.LegalKnowledge;
using Evidata.Modules.ProcessingInventory;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.Reporting;
using Evidata.Modules.Search;
using Evidata.Modules.Security;
using Evidata.Modules.TenantManagement;
using Evidata.Modules.Workflow;
using Evidata.Worker.Outbox.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Evidata.Tests.Unit.Api.Services;

/// <summary>
/// Integration test for ProcessingActivityRiskAssessmentService DI resolution.
/// Verifies that the service can be resolved from a properly configured service container
/// and that all its dependencies are correctly registered.
/// 
/// This test is critical because it catches DI configuration errors that would only appear
/// at runtime (e.g., missing service registrations, circular dependencies).
/// </summary>
public sealed class ProcessingActivityRiskAssessmentServiceDependencyInjectionTests
{
    [Fact]
    public void ServiceResolution_WithAllModulesConfigured_ShouldSucceed()
    {
        // Arrange: Build a service collection that mimics production configuration
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add configuration with dummy values
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:evidata-db", "Server=localhost;Database=evidata_test;"},
                {"Jwt:Issuer", "https://test.issuer"},
                {"Jwt:Audience", "test-audience"},
                {"Jwt:SigningKey", "test-key-that-is-at-least-32-characters-long-for-HS256"}
            })
            .Build();
        
        // Register all modules that the service depends on
        services.AddTenantManagement(config);
        services.AddRbac(config);
        services.AddAudit(config);
        services.AddDocumentsModule(config);
        services.AddLegalKnowledge(config);
        services.AddEvidenceModule(config);
        services.AddProcessingInventoryModule(config);
        services.AddGapManagementModule(config);
        services.AddWorkflowModule(config);
        services.AddReportingModule(config);
        services.AddSearchModule(config);
        
        // Add DbContext for Outbox
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseInMemoryDatabase("evidata_test"));
        services.AddScoped<OutboxRepository>();
        services.AddScoped<IOutboxWriter>(sp => 
            sp.GetRequiredService<OutboxRepository>());
        
        // Register the services that the test targets
        services.AddScoped<ProcessingActivityRiskAssessmentService>();
        services.AddScoped<IProcessingActivityRiskAssessmentService>(sp =>
            sp.GetRequiredService<ProcessingActivityRiskAssessmentService>());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act: Attempt to resolve the service
        var service = serviceProvider.GetRequiredService<IProcessingActivityRiskAssessmentService>();
        
        // Assert: Service should be resolved without throwing
        Assert.NotNull(service);
        Assert.IsType<ProcessingActivityRiskAssessmentService>(service);
    }
    
    [Fact]
    public void DependenciesResolution_AllRequiredServicesPresent_ShouldResolveSuccessfully()
    {
        // Arrange: Same setup as above, but verify that all dependencies are registered
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:evidata-db", "Server=localhost;Database=evidata_test;"},
                {"Jwt:Issuer", "https://test.issuer"},
                {"Jwt:Audience", "test-audience"},
                {"Jwt:SigningKey", "test-key-that-is-at-least-32-characters-long-for-HS256"}
            })
            .Build();
        
        services.AddTenantManagement(config);
        services.AddRbac(config);
        services.AddAudit(config);
        services.AddDocumentsModule(config);
        services.AddLegalKnowledge(config);
        services.AddEvidenceModule(config);
        services.AddProcessingInventoryModule(config);
        services.AddGapManagementModule(config);
        services.AddWorkflowModule(config);
        services.AddReportingModule(config);
        services.AddSearchModule(config);
        
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseInMemoryDatabase("evidata_test"));
        services.AddScoped<OutboxRepository>();
        services.AddScoped<IOutboxWriter>(sp => 
            sp.GetRequiredService<OutboxRepository>());
        
        services.AddScoped<ProcessingActivityRiskAssessmentService>();
        services.AddScoped<IProcessingActivityRiskAssessmentService>(sp =>
            sp.GetRequiredService<ProcessingActivityRiskAssessmentService>());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act: Verify that all required dependencies can be resolved
        var gapService = serviceProvider.GetRequiredService<Evidata.Modules.GapManagement.Application.Abstractions.IGapSummaryQueryService>();
        var evidenceService = serviceProvider.GetRequiredService<Evidata.Modules.Evidence.Application.Abstractions.IEvidenceSummaryQueryService>();
        var reviewService = serviceProvider.GetRequiredService<Evidata.Modules.Workflow.Application.Abstractions.IReviewSummaryQueryService>();
        
        // Assert: All services should be resolved
        Assert.NotNull(gapService);
        Assert.NotNull(evidenceService);
        Assert.NotNull(reviewService);
    }
}

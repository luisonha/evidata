using Evidata.Functions.Reporting.Handlers;
using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Tests.Unit.Reporting;

public class RatReportHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ProcessingActivity BuildActivity(string name = "Tratamiento Test")
    {
        var a = ProcessingActivity.Create(TenantId, name, Guid.NewGuid());
        return a;
    }

    // ── GenerateExcel — estructura ────────────────────────────────────────────

    [Fact]
    public void GenerateExcel_EmptyList_ReturnsNonEmptyBytes()
    {
        var bytes = RatReportHandler.GenerateExcel([], TenantId);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateExcel_WithActivities_ReturnsNonEmptyBytes()
    {
        var activities = new[]
        {
            BuildActivity("Marketing digital"),
            BuildActivity("Facturación electrónica")
        };

        var bytes = RatReportHandler.GenerateExcel(activities, TenantId);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateExcel_OutputIsValidExcel()
    {
        var activities = new[] { BuildActivity() };
        var bytes = RatReportHandler.GenerateExcel(activities, TenantId);

        // Un workbook xlsx empieza con la firma PK (zip)
        Assert.Equal(0x50, bytes[0]); // 'P'
        Assert.Equal(0x4B, bytes[1]); // 'K'
    }

    [Fact]
    public void GenerateExcel_MultipleActivities_SizeGrowsWithData()
    {
        var emptyBytes = RatReportHandler.GenerateExcel([], TenantId);

        var activities = Enumerable.Range(1, 10)
            .Select(i => BuildActivity($"Tratamiento {i}"))
            .ToList();
        var filledBytes = RatReportHandler.GenerateExcel(activities, TenantId);

        // Con más datos el archivo debería ser más grande
        Assert.True(filledBytes.Length >= emptyBytes.Length);
    }

    [Fact]
    public void GenerateExcel_WithTransferFlag_DoesNotThrow()
    {
        var activity = BuildActivity("Transferencia int.");
        activity.SetRiskInputs(
            hasInternationalTransfer: true,
            hasAutomatedDecision: false,
            modifiedBy: Guid.NewGuid());

        var bytes = RatReportHandler.GenerateExcel([activity], TenantId);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateExcel_WithDataCategories_DoesNotThrow()
    {
        var activity = BuildActivity("Con categorías");
        activity.SetDataCategories(
            new[] { DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary) },
            Guid.NewGuid());

        var bytes = RatReportHandler.GenerateExcel([activity], TenantId);
        Assert.NotEmpty(bytes);
    }

    // ── ReportingModels ───────────────────────────────────────────────────────

    [Fact]
    public void ReportJobRequestPayload_Deserialization_Works()
    {
        var json = """
            {
              "jobId": "11111111-1111-1111-1111-111111111111",
              "tenantId": "22222222-2222-2222-2222-222222222222",
              "reportType": "RAT",
              "parameters": "{}",
              "requestedBy": "33333333-3333-3333-3333-333333333333"
            }
            """;

        var payload = System.Text.Json.JsonSerializer.Deserialize<
            Evidata.Functions.Reporting.Models.ReportJobRequestPayload>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(payload);
        Assert.Equal("RAT", payload.ReportType);
        Assert.Equal(new Guid("11111111-1111-1111-1111-111111111111"), payload.JobId);
    }
}

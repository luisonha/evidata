using Evidata.Api.Infrastructure.HealthChecks;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents;
using Evidata.Modules.Evidence;
using Evidata.Modules.GapManagement;
using Evidata.Modules.Identity;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Evidata.Modules.LegalKnowledge;
using Evidata.Modules.ProcessingInventory;
using Evidata.Modules.Security;
using Evidata.Modules.TenantManagement;
using Evidata.Modules.Mcp;
using Evidata.Modules.Reporting;
using Evidata.Modules.Search;
using Evidata.Modules.Workflow;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEvidataHealthChecks(builder.Configuration);
builder.Services.AddTenantManagement(builder.Configuration);
builder.Services.AddIdentityBridge(builder.Configuration, builder.Environment);
builder.Services.AddRbac(builder.Configuration);
builder.Services.AddAudit(builder.Configuration);
builder.Services.AddDocumentsModule(builder.Configuration);
builder.Services.AddLegalKnowledge(builder.Configuration);
builder.Services.AddEvidenceModule(builder.Configuration);
builder.Services.AddProcessingInventoryModule(builder.Configuration);
builder.Services.AddGapManagementModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddSearchModule(builder.Configuration);
builder.Services.AddMcpModule(builder.Configuration);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapEvidataHealthEndpoints();
app.UseLocalDevGuard();
app.UseTenantIsolation();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

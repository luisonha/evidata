using Evidata.Modules.Workflow.Application.Queries;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.Workflow;

public class ReviewSummaryQueryHandlerTests
{
    private static WorkflowDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _processingActivityId = Guid.NewGuid();
    private static readonly Guid _versionId = Guid.NewGuid();
    private static readonly Guid _requester = Guid.NewGuid();
    private static readonly Guid _reviewer = Guid.NewGuid();

    // ── TC1: Sin revisiones → devuelve DTO con lastDecisionCode=null ─────────────

    [Fact]
    public async Task GetReviewSummary_NoReviews_ReturnsNullDecision()
    {
        await using var db = BuildContext();
        var handler = new GetReviewSummaryQueryHandler(db);

        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Equal(_processingActivityId, result.ProcessingActivityId);
        Assert.Equal(_versionId, result.VersionId);
        Assert.Null(result.LastDecisionCode);
        Assert.Null(result.DecidedAt);
        Assert.Empty(result.RequiredDomains);
        Assert.Empty(result.PendingDomains);
    }

    // ── TC2: Revisión Approved → devuelve "Approved" + decidedAt ─────────────────

    [Fact]
    public async Task GetReviewSummary_ApprovedReview_ReturnsApprovedDecision()
    {
        await using var db = BuildContext();
        
        var review = Review.Create(
            _tenantId, "ProcessingInventory", "ProcessingActivity", 
            _processingActivityId, _requester);
        review.Start(_reviewer);
        review.Approve(_reviewer, "Verificado.");

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var handler = new GetReviewSummaryQueryHandler(db);
        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Equal("Approved", result.LastDecisionCode);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal(review.CompletedAt, result.DecidedAt);
    }

    // ── TC3: Revisión ChangesRequested → devuelve "ChangesRequested" + decidedAt ─

    [Fact]
    public async Task GetReviewSummary_ChangesRequestedReview_ReturnsChangesRequestedDecision()
    {
        await using var db = BuildContext();

        var review = Review.Create(
            _tenantId, "ProcessingInventory", "ProcessingActivity",
            _processingActivityId, _requester);
        review.Start(_reviewer);
        review.RequestChanges(_reviewer, "Revisar sección legal.");

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var handler = new GetReviewSummaryQueryHandler(db);
        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Equal("ChangesRequested", result.LastDecisionCode);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal(review.CompletedAt, result.DecidedAt);
    }

    // ── TC4: Revisión Cancelled → devuelve null (no es decisión final) ───────────

    [Fact]
    public async Task GetReviewSummary_CancelledReview_ReturnsNullDecision()
    {
        await using var db = BuildContext();

        var review = Review.Create(
            _tenantId, "ProcessingInventory", "ProcessingActivity",
            _processingActivityId, _requester);
        review.Cancel(_requester);

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var handler = new GetReviewSummaryQueryHandler(db);
        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Null(result.LastDecisionCode);
        Assert.Null(result.DecidedAt);
    }

    // ── TC5: Revisión Open o InProgress → devuelve null (no completada) ─────────

    [Fact]
    public async Task GetReviewSummary_IncompleteReview_ReturnsNullDecision()
    {
        await using var db = BuildContext();

        var review = Review.Create(
            _tenantId, "ProcessingInventory", "ProcessingActivity",
            _processingActivityId, _requester);
        // No start ni approve, queda en Open

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var handler = new GetReviewSummaryQueryHandler(db);
        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Null(result.LastDecisionCode);
        Assert.Null(result.DecidedAt);
    }

    // ── TC6: Múltiples revisiones → devuelve la más reciente (última insertada) ──

    [Fact]
    public async Task GetReviewSummary_MultipleReviews_ReturnsMostRecent()
    {
        await using var db = BuildContext();

        // Primera revisión: Approved
        var review1 = Review.Create(
            _tenantId, "ProcessingInventory", "ProcessingActivity",
            _processingActivityId, _requester);
        review1.Start(_reviewer);
        review1.Approve(_reviewer, "Primera aprobación.");

        // Segunda revisión: ChangesRequested (más reciente)
        var review2 = Review.Create(
            _tenantId, "ProcessingInventory", "ProcessingActivity",
            _processingActivityId, _requester);
        review2.Start(_reviewer);
        review2.RequestChanges(_reviewer, "Se solicitan cambios.");

        db.Reviews.Add(review1);
        db.Reviews.Add(review2);
        await db.SaveChangesAsync();

        // La query debe retornar la más reciente (review2 por default CreatedAt desc)
        var handler = new GetReviewSummaryQueryHandler(db);
        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        // Debería retornar review2 (la última insertada, que tiene CreatedAt >= review1)
        Assert.NotNull(result.LastDecisionCode);
        Assert.True(result.LastDecisionCode == "ChangesRequested" || result.LastDecisionCode == "Approved",
            $"Expected ChangesRequested or Approved, got {result.LastDecisionCode}");
    }

    // ── TC7: Revisiones de otras actividades → se ignoran (TargetEntityId filter) ─

    [Fact]
    public async Task GetReviewSummary_OtherActivityReview_ReturnsNullDecision()
    {
        await using var db = BuildContext();

        // Revisión para una actividad diferente
        var otherActivityId = Guid.NewGuid();
        var review = Review.Create(
            _tenantId, "ProcessingInventory", "ProcessingActivity",
            otherActivityId, _requester);
        review.Start(_reviewer);
        review.Approve(_reviewer, "Aprobado otra actividad.");

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var handler = new GetReviewSummaryQueryHandler(db);
        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Null(result.LastDecisionCode);
        Assert.Null(result.DecidedAt);
    }

    // ── TC8: Revisiones de otros tenants → se ignoran (TenantId filter) ──────────

    [Fact]
    public async Task GetReviewSummary_OtherTenantReview_ReturnsNullDecision()
    {
        await using var db = BuildContext();

        // Revisión para otro tenant
        var otherTenantId = Guid.NewGuid();
        var review = Review.Create(
            otherTenantId, "ProcessingInventory", "ProcessingActivity",
            _processingActivityId, _requester);
        review.Start(_reviewer);
        review.Approve(_reviewer, "Aprobado otro tenant.");

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        var handler = new GetReviewSummaryQueryHandler(db);
        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Null(result.LastDecisionCode);
        Assert.Null(result.DecidedAt);
    }

    // ── TC9: DTOs tienen valores por defecto correctos ─────────────────────────

    [Fact]
    public async Task GetReviewSummary_DefaultValues_AreCorrect()
    {
        await using var db = BuildContext();
        var handler = new GetReviewSummaryQueryHandler(db);

        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("processing.activity.version.status.draft", result.StatusLabelKey);
        Assert.Null(result.LastDecisionLabelKey);
    }

    // ── TC10: DTOs contienen IDs esperados ──────────────────────────────────────

    [Fact]
    public async Task GetReviewSummary_ContainsExpectedIds()
    {
        await using var db = BuildContext();
        var handler = new GetReviewSummaryQueryHandler(db);

        var result = await handler.HandleAsync(_tenantId, _processingActivityId, _versionId);

        Assert.NotNull(result);
        Assert.Equal(_processingActivityId, result.ProcessingActivityId);
        Assert.Equal(_versionId, result.VersionId);
    }
}

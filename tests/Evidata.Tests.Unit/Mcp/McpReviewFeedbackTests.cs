using Evidata.Modules.Mcp.Domain;

namespace Evidata.Tests.Unit.Mcp;

public class McpReviewTaskTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _interaction = Guid.NewGuid();
    private static readonly Guid _reviewer = Guid.NewGuid();

    private static McpReviewTask Build() => McpReviewTask.Create(_interaction, _tenant);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsOpen()
    {
        var t = Build();
        Assert.Equal(McpReviewTaskStatus.Open, t.Status);
        Assert.Equal(_interaction, t.InteractionId);
        Assert.Equal(_tenant, t.TenantId);
        Assert.Null(t.AssignedTo);
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_SetsInProgress_AndAssignsReviewer()
    {
        var t = Build();
        t.Start(_reviewer);
        Assert.Equal(McpReviewTaskStatus.InProgress, t.Status);
        Assert.Equal(_reviewer, t.AssignedTo);
        Assert.NotNull(t.StartedAt);
    }

    [Fact]
    public void Start_NotOpen_Throws()
    {
        var t = Build();
        t.Start(_reviewer);
        Assert.Throws<InvalidOperationException>(() => t.Start(_reviewer));
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_SetsApproved()
    {
        var t = Build();
        t.Start(_reviewer);
        t.Approve(_reviewer, "Respuesta correcta según normativa vigente.");
        Assert.Equal(McpReviewTaskStatus.Approved, t.Status);
        Assert.Equal("Respuesta correcta según normativa vigente.", t.ReviewNotes);
        Assert.NotNull(t.CompletedAt);
    }

    [Fact]
    public void Approve_NoNotes_IsAllowed()
    {
        var t = Build();
        t.Start(_reviewer);
        t.Approve(_reviewer);
        Assert.Equal(McpReviewTaskStatus.Approved, t.Status);
        Assert.Null(t.ReviewNotes);
    }

    [Fact]
    public void Approve_WrongReviewer_Throws()
    {
        var t = Build();
        t.Start(_reviewer);
        Assert.Throws<InvalidOperationException>(() => t.Approve(Guid.NewGuid()));
    }

    [Fact]
    public void Approve_NotInProgress_Throws()
    {
        var t = Build();
        Assert.Throws<InvalidOperationException>(() => t.Approve(_reviewer));
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_SetsRejected()
    {
        var t = Build();
        t.Start(_reviewer);
        t.Reject(_reviewer, "Respuesta incorrecta — datos desactualizados.");
        Assert.Equal(McpReviewTaskStatus.Rejected, t.Status);
        Assert.Equal("Respuesta incorrecta — datos desactualizados.", t.ReviewNotes);
        Assert.NotNull(t.CompletedAt);
    }

    [Fact]
    public void Reject_EmptyNotes_Throws()
    {
        var t = Build();
        t.Start(_reviewer);
        Assert.Throws<ArgumentException>(() => t.Reject(_reviewer, ""));
    }

    [Fact]
    public void Reject_WrongReviewer_Throws()
    {
        var t = Build();
        t.Start(_reviewer);
        Assert.Throws<InvalidOperationException>(() => t.Reject(Guid.NewGuid(), "Razón."));
    }
}

public class McpFeedbackTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _user = Guid.NewGuid();
    private static readonly Guid _interaction = Guid.NewGuid();

    // ── Record ────────────────────────────────────────────────────────────────

    [Fact]
    public void Record_Helpful_SetsFields()
    {
        var f = McpFeedback.Record(_interaction, _tenant, _user, McpFeedbackRating.Helpful, "Muy útil.");
        Assert.Equal(_interaction, f.InteractionId);
        Assert.Equal(_tenant, f.TenantId);
        Assert.Equal(_user, f.UserId);
        Assert.Equal(McpFeedbackRating.Helpful, f.Rating);
        Assert.Equal("Muy útil.", f.Comment);
        Assert.NotEqual(Guid.Empty, f.Id);
    }

    [Fact]
    public void Record_NotHelpful_NoComment_IsValid()
    {
        var f = McpFeedback.Record(_interaction, _tenant, _user, McpFeedbackRating.NotHelpful);
        Assert.Equal(McpFeedbackRating.NotHelpful, f.Rating);
        Assert.Null(f.Comment);
    }

    [Fact]
    public void Record_TrimsComment()
    {
        var f = McpFeedback.Record(_interaction, _tenant, _user, McpFeedbackRating.Helpful, "  comentario  ");
        Assert.Equal("comentario", f.Comment);
    }
}

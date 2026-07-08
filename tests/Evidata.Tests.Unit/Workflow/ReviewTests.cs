using Evidata.Modules.Workflow.Domain;

namespace Evidata.Tests.Unit.Workflow;

public class ReviewTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _requester = Guid.NewGuid();
    private static readonly Guid _reviewer = Guid.NewGuid();
    private static readonly Guid _entity = Guid.NewGuid();

    private static Review Build() =>
        Review.Create(_tenant, "ProcessingInventory", "ProcessingActivity", _entity, _requester);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsStatusOpen()
    {
        var r = Build();
        Assert.Equal(ReviewStatus.Open, r.Status);
    }

    [Fact]
    public void Create_SetsRequiredFields()
    {
        var r = Build();
        Assert.Equal(_tenant, r.TenantId);
        Assert.Equal("ProcessingInventory", r.TargetModule);
        Assert.Equal("ProcessingActivity", r.TargetEntityType);
        Assert.Equal(_entity, r.TargetEntityId);
        Assert.Equal(_requester, r.RequestedBy);
        Assert.NotEqual(Guid.Empty, r.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyModule_Throws(string module)
    {
        Assert.Throws<ArgumentException>(() =>
            Review.Create(_tenant, module, "T", _entity, _requester));
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_FromOpen_SetsInProgress()
    {
        var r = Build();
        r.Start(_reviewer);
        Assert.Equal(ReviewStatus.InProgress, r.Status);
        Assert.Equal(_reviewer, r.ReviewerId);
        Assert.NotNull(r.StartedAt);
    }

    [Fact]
    public void Start_NotOpen_Throws()
    {
        var r = Build();
        r.Start(_reviewer);
        Assert.Throws<InvalidOperationException>(() => r.Start(_reviewer));
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_InProgress_SetsApproved()
    {
        var r = Build();
        r.Start(_reviewer);
        r.Approve(_reviewer, "Correcto.");

        Assert.Equal(ReviewStatus.Approved, r.Status);
        Assert.Equal("Correcto.", r.Comments);
        Assert.NotNull(r.CompletedAt);
    }

    [Fact]
    public void Approve_WrongReviewer_Throws()
    {
        var r = Build();
        r.Start(_reviewer);
        Assert.Throws<InvalidOperationException>(() => r.Approve(Guid.NewGuid()));
    }

    [Fact]
    public void Approve_NotInProgress_Throws()
    {
        var r = Build();
        Assert.Throws<InvalidOperationException>(() => r.Approve(_reviewer));
    }

    // ── RequestChanges ────────────────────────────────────────────────────────

    [Fact]
    public void RequestChanges_InProgress_SetsChangesRequested()
    {
        var r = Build();
        r.Start(_reviewer);
        r.RequestChanges(_reviewer, "Falta sección de retención.");

        Assert.Equal(ReviewStatus.ChangesRequested, r.Status);
        Assert.Equal("Falta sección de retención.", r.Comments);
        Assert.NotNull(r.CompletedAt);
    }

    [Fact]
    public void RequestChanges_EmptyComments_Throws()
    {
        var r = Build();
        r.Start(_reviewer);
        Assert.Throws<ArgumentException>(() => r.RequestChanges(_reviewer, ""));
    }

    [Fact]
    public void RequestChanges_WrongReviewer_Throws()
    {
        var r = Build();
        r.Start(_reviewer);
        Assert.Throws<InvalidOperationException>(() =>
            r.RequestChanges(Guid.NewGuid(), "Comentario."));
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_FromOpen_SetsCancelled()
    {
        var r = Build();
        r.Cancel(Guid.NewGuid());
        Assert.Equal(ReviewStatus.Cancelled, r.Status);
        Assert.NotNull(r.CompletedAt);
    }

    [Fact]
    public void Cancel_FromInProgress_SetsCancelled()
    {
        var r = Build();
        r.Start(_reviewer);
        r.Cancel(Guid.NewGuid());
        Assert.Equal(ReviewStatus.Cancelled, r.Status);
    }

    [Fact]
    public void Cancel_Approved_Throws()
    {
        var r = Build();
        r.Start(_reviewer);
        r.Approve(_reviewer);
        Assert.Throws<InvalidOperationException>(() => r.Cancel(Guid.NewGuid()));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var r = Build();
        r.Cancel(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => r.Cancel(Guid.NewGuid()));
    }

    // ── Flujo completo ────────────────────────────────────────────────────────

    [Fact]
    public void FullApprovalFlow_ProducesCorrectState()
    {
        var r = Build();

        Assert.Equal(ReviewStatus.Open, r.Status);
        r.Start(_reviewer);
        Assert.Equal(ReviewStatus.InProgress, r.Status);
        Assert.Equal(_reviewer, r.ReviewerId);
        r.Approve(_reviewer, "Verificado.");
        Assert.Equal(ReviewStatus.Approved, r.Status);
        Assert.Equal("Verificado.", r.Comments);
    }

    [Fact]
    public void ChangesRequestedFlow_ProducesCorrectState()
    {
        var r = Build();
        r.Start(_reviewer);
        r.RequestChanges(_reviewer, "Revisar sección legal.");
        Assert.Equal(ReviewStatus.ChangesRequested, r.Status);
        Assert.NotNull(r.Comments);
    }
}

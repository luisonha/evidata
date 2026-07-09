using Evidata.Modules.Workflow.Domain;

namespace Evidata.Tests.Unit.Workflow;

public class WorkflowTaskTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _assignee = Guid.NewGuid();
    private static readonly Guid _creator = Guid.NewGuid();
    private static readonly Guid _entity = Guid.NewGuid();

    private static WorkflowTask Build(DateTimeOffset? dueAt = null) =>
        WorkflowTask.Create(_tenant, "ProcessingInventory", "ProcessingActivity",
            _entity, WorkflowTaskType.Review, "Revisar RAT", _assignee, _creator, dueAt);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsStatusOpen()
    {
        var t = Build();
        Assert.Equal(WorkflowTaskStatus.Open, t.Status);
    }

    [Fact]
    public void Create_SetsRequiredFields()
    {
        var t = Build();
        Assert.Equal(_tenant, t.TenantId);
        Assert.Equal("ProcessingInventory", t.TargetModule);
        Assert.Equal("ProcessingActivity", t.TargetEntityType);
        Assert.Equal(_entity, t.TargetEntityId);
        Assert.Equal(WorkflowTaskType.Review, t.TaskType);
        Assert.Equal("Revisar RAT", t.Title);
        Assert.Equal(_assignee, t.AssignedTo);
        Assert.NotEqual(Guid.Empty, t.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyTitle_Throws(string title)
    {
        Assert.Throws<ArgumentException>(() =>
            WorkflowTask.Create(_tenant, "M", "T", _entity, WorkflowTaskType.Review,
                title, _assignee, _creator));
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_FromOpen_SetsInProgress()
    {
        var t = Build();
        t.Start(_creator);
        Assert.Equal(WorkflowTaskStatus.InProgress, t.Status);
        Assert.NotNull(t.StartedAt);
    }

    [Fact]
    public void Start_NotOpen_Throws()
    {
        var t = Build();
        t.Start(_creator);
        Assert.Throws<InvalidOperationException>(() => t.Start(_creator));
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_InProgress_SetsCompleted()
    {
        var t = Build();
        t.Start(_creator);
        t.Complete(_creator);
        Assert.Equal(WorkflowTaskStatus.Completed, t.Status);
        Assert.NotNull(t.CompletedAt);
    }

    [Fact]
    public void Complete_NotInProgress_Throws()
    {
        var t = Build();
        Assert.Throws<InvalidOperationException>(() => t.Complete(_creator));
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_FromOpen_SetsCancelled()
    {
        var t = Build();
        t.Cancel(_creator);
        Assert.Equal(WorkflowTaskStatus.Cancelled, t.Status);
    }

    [Fact]
    public void Cancel_FromInProgress_SetsCancelled()
    {
        var t = Build();
        t.Start(_creator);
        t.Cancel(_creator);
        Assert.Equal(WorkflowTaskStatus.Cancelled, t.Status);
    }

    [Fact]
    public void Cancel_Completed_Throws()
    {
        var t = Build();
        t.Start(_creator);
        t.Complete(_creator);
        Assert.Throws<InvalidOperationException>(() => t.Cancel(_creator));
    }

    // ── MarkOverdue ───────────────────────────────────────────────────────────

    [Fact]
    public void MarkOverdue_OpenTask_SetsOverdue()
    {
        var t = Build();
        t.MarkOverdue();
        Assert.Equal(WorkflowTaskStatus.Overdue, t.Status);
    }

    [Fact]
    public void MarkOverdue_Completed_IsIdempotent()
    {
        var t = Build();
        t.Start(_creator);
        t.Complete(_creator);
        t.MarkOverdue(); // no lanza excepción
        Assert.Equal(WorkflowTaskStatus.Completed, t.Status);
    }

    [Fact]
    public void MarkOverdue_AlreadyOverdue_IsIdempotent()
    {
        var t = Build();
        t.MarkOverdue();
        t.MarkOverdue(); // no lanza
        Assert.Equal(WorkflowTaskStatus.Overdue, t.Status);
    }

    // ── Reassign ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reassign_Open_ChangesAssignee()
    {
        var t = Build();
        var newAssignee = Guid.NewGuid();
        t.Reassign(newAssignee, _creator);
        Assert.Equal(newAssignee, t.AssignedTo);
    }

    [Fact]
    public void Reassign_Completed_Throws()
    {
        var t = Build();
        t.Start(_creator);
        t.Complete(_creator);
        Assert.Throws<InvalidOperationException>(() => t.Reassign(Guid.NewGuid(), _creator));
    }

    // ── UpdateDueDate ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateDueDate_Open_UpdatesDate()
    {
        var t = Build();
        var newDate = DateTimeOffset.UtcNow.AddDays(10);
        t.UpdateDueDate(newDate, _creator);
        Assert.Equal(newDate, t.DueAt);
    }

    [Fact]
    public void UpdateDueDate_Cancelled_Throws()
    {
        var t = Build();
        t.Cancel(_creator);
        Assert.Throws<InvalidOperationException>(() =>
            t.UpdateDueDate(DateTimeOffset.UtcNow.AddDays(5), _creator));
    }

    // ── IsOverdue ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsOverdue_PastDueOpen_ReturnsTrue()
    {
        var due = DateTimeOffset.UtcNow.AddDays(-1);
        var t = Build(dueAt: due);
        Assert.True(t.IsOverdue(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsOverdue_FutureDue_ReturnsFalse()
    {
        var due = DateTimeOffset.UtcNow.AddDays(5);
        var t = Build(dueAt: due);
        Assert.False(t.IsOverdue(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsOverdue_NoDueDate_ReturnsFalse()
    {
        var t = Build();
        Assert.False(t.IsOverdue(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsOverdue_CompletedPastDue_ReturnsFalse()
    {
        var due = DateTimeOffset.UtcNow.AddDays(-1);
        var t = Build(dueAt: due);
        t.Start(_creator);
        t.Complete(_creator);
        Assert.False(t.IsOverdue(DateTimeOffset.UtcNow));
    }
}

using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Notifications;
using Evidata.Modules.Contracts.Notifications;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Workflow.Infrastructure.Reviews;

/// <summary>
/// Implementación de <see cref="IReviewService"/> con EF Core + PostgreSQL.
/// Emits domain events via IReviewNotificationService cuando las reviews transicionan.
/// 
/// P1-017: When review is approved, invokes registered event handlers
/// (e.g., ProcessingInventory IReviewEventHandler) to update related entities.
/// 
/// P1-019: Refactored to use direct DI injection instead of reflection-based
/// service locator pattern for type safety and cleaner architecture.
/// </summary>
public sealed class ReviewService : IReviewService
{
    private readonly WorkflowDbContext _db;
    private readonly IReviewNotificationService _notificationService;
    private readonly IReviewEventHandler _handler;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        WorkflowDbContext db,
        IReviewNotificationService notificationService,
        IReviewEventHandler handler,
        ILogger<ReviewService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _handler = handler;
        _logger = logger;
    }

    public async Task<Review> CreateAsync(
        Guid tenantId, string targetModule, string targetEntityType,
        Guid targetEntityId, Guid requestedBy, CancellationToken ct = default)
    {
        var review = Review.Create(tenantId, targetModule, targetEntityType,
            targetEntityId, requestedBy);

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);
        return review;
    }

    public async Task StartAsync(Guid reviewId, Guid reviewerId, CancellationToken ct = default)
    {
        var review = await GetRequiredAsync(reviewId, ct);
        review.Start(reviewerId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApproveAsync(Guid reviewId, Guid reviewerId, string? comments = null, CancellationToken ct = default)
    {
        var review = await GetRequiredAsync(reviewId, ct);
        review.Approve(reviewerId, comments);
        await _db.SaveChangesAsync(ct);
        
        // P1-017: Emit event to notify downstream modules (e.g., ProcessingInventory) that review is approved
        // Event is written to Outbox for async processing or can be consumed synchronously by registered handlers
        await _notificationService.NotifyApprovedAsync(review, ct);

        // P1-019: Invoke handler directly via DI (no reflection)
        await InvokeReviewEventHandlerAsync(review, ct);
    }

    private async Task InvokeReviewEventHandlerAsync(Review review, CancellationToken ct)
    {
        try
        {
            // P1-019: Direct DI injection — no reflection, compile-time safe
            var payload = new ReviewApprovedEventPayload(
                review.Id,
                review.TenantId,
                review.TargetModule,
                review.TargetEntityType,
                review.TargetEntityId,
                review.ReviewerId ?? Guid.Empty,
                review.Comments,
                DateTimeOffset.UtcNow,
                null);

            await _handler.HandleReviewApprovedAsync(payload, ct);
            
            _logger.LogInformation(
                "Successfully invoked review event handler for review {ReviewId}.",
                review.Id);
        }
        catch (Exception ex)
        {
            // Log but don't fail — handler errors are visible but don't block approval.
            // Outbox will ensure eventual consistency for async consumers.
            _logger.LogError(ex,
                "Error invoking review event handler for review {ReviewId}. " +
                "Continuing with Outbox-only delivery. " +
                "TargetEntity: {TargetModule}/{TargetEntityType}/{TargetEntityId}",
                review.Id,
                review.TargetModule,
                review.TargetEntityType,
                review.TargetEntityId);
        }
    }

    public async Task RequestChangesAsync(Guid reviewId, Guid reviewerId, string comments, CancellationToken ct = default)
    {
        var review = await GetRequiredAsync(reviewId, ct);
        review.RequestChanges(reviewerId, comments);
        await _db.SaveChangesAsync(ct);
        
        // Emit event to notify downstream modules that changes are requested
        await _notificationService.NotifyChangesRequestedAsync(review, ct);
    }

    public async Task CancelAsync(Guid reviewId, Guid cancelledBy, CancellationToken ct = default)
    {
        var review = await GetRequiredAsync(reviewId, ct);
        review.Cancel(cancelledBy);
        await _db.SaveChangesAsync(ct);
        
        // Emit event to notify downstream modules that review is cancelled
        await _notificationService.NotifyCancelledAsync(review, ct);
    }

    public Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken ct = default) =>
        _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);

    public async Task<IReadOnlyList<Review>> GetOpenReviewsForEntityAsync(
        Guid tenantId, Guid targetEntityId, CancellationToken ct = default)
    {
        return await _db.Reviews
            .Where(r => r.TenantId == tenantId
                     && r.TargetEntityId == targetEntityId
                     && (r.Status == ReviewStatus.Open || r.Status == ReviewStatus.InProgress))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    private async Task<Review> GetRequiredAsync(Guid reviewId, CancellationToken ct)
    {
        return await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct)
            ?? throw new KeyNotFoundException($"Review {reviewId} no encontrada.");
    }
}

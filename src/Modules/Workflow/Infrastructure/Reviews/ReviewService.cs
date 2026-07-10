using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Notifications;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Workflow.Infrastructure.Reviews;

/// <summary>
/// Implementación de <see cref="IReviewService"/> con EF Core + PostgreSQL.
/// Emits domain events via IReviewNotificationService cuando las reviews transicionan.
/// 
/// P1-017: When review is approved, also tries to invoke registered event handlers
/// (e.g., ProcessingInventory IReviewEventHandler) to update related entities.
/// Uses IServiceProvider for late binding to avoid circular dependencies.
/// </summary>
public sealed class ReviewService : IReviewService
{
    private readonly WorkflowDbContext _db;
    private readonly IReviewNotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    public ReviewService(
        WorkflowDbContext db,
        IReviewNotificationService notificationService,
        IServiceProvider serviceProvider)
    {
        _db = db;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
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

        // P1-017: Try to invoke ProcessingInventory or other module handlers synchronously
        // This is safe because ProcessingInventory module is already registered when Workflow is loaded
        await TryInvokeReviewEventHandlerAsync(review, ct);
    }

    private async Task TryInvokeReviewEventHandlerAsync(Review review, CancellationToken ct)
    {
        try
        {
            // Try to resolve IReviewEventHandler from the container
            // This is available if ProcessingInventory module is loaded
            var handlerType = Type.GetType(
                "Evidata.Modules.ProcessingInventory.Application.Abstractions.IReviewEventHandler, Evidata.Modules.ProcessingInventory");
            
            if (handlerType == null)
                return; // Handler type not found — ProcessingInventory module not loaded or type not available

            var handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
                return; // Handler not registered in DI container

            // Build the payload
            var payloadType = Type.GetType(
                "Evidata.Modules.Workflow.Application.Notifications.ReviewApprovedEventPayload, Evidata.Modules.Workflow");
            
            if (payloadType == null)
                return;

            // Create payload instance
            var payload = Activator.CreateInstance(payloadType,
                review.Id,
                review.TenantId,
                review.TargetModule,
                review.TargetEntityType,
                review.TargetEntityId,
                review.ReviewerId ?? Guid.Empty,
                review.Comments,
                DateTimeOffset.UtcNow,
                review.RequestedBy);

            if (payload == null)
                return;

            // Invoke HandleReviewApprovedAsync
            var method = handlerType.GetMethod("HandleReviewApprovedAsync");
            if (method != null)
            {
                var task = (Task?)method.Invoke(handler, new[] { payload, ct });
                if (task != null)
                {
                    await task.ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail — missing or broken handler is not fatal
            System.Diagnostics.Debug.WriteLine($"Error invoking review event handler: {ex.Message}");
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

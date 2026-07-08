using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Workflow.Infrastructure.Reviews;

/// <summary>
/// Implementación de <see cref="IReviewService"/> con EF Core + PostgreSQL.
/// </summary>
public sealed class ReviewService : IReviewService
{
    private readonly WorkflowDbContext _db;

    public ReviewService(WorkflowDbContext db)
    {
        _db = db;
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
    }

    public async Task RequestChangesAsync(Guid reviewId, Guid reviewerId, string comments, CancellationToken ct = default)
    {
        var review = await GetRequiredAsync(reviewId, ct);
        review.RequestChanges(reviewerId, comments);
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid reviewId, Guid cancelledBy, CancellationToken ct = default)
    {
        var review = await GetRequiredAsync(reviewId, ct);
        review.Cancel(cancelledBy);
        await _db.SaveChangesAsync(ct);
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

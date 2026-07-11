using Evidata.Modules.Workflow.Domain;

namespace Evidata.Modules.Workflow.Application.Abstractions;

/// <summary>
/// Servicio de revisiones humanas sobre entidades de cualquier módulo.
/// </summary>
public interface IReviewService
{
    /// <summary>Abre una nueva revisión para una entidad.</summary>
    Task<Review> CreateAsync(
        Guid tenantId,
        string targetModule,
        string targetEntityType,
        Guid targetEntityId,
        Guid requestedBy,
        int reviewDomain = 0,
        CancellationToken ct = default);

    /// <summary>Asigna revisor e inicia la revisión.</summary>
    Task StartAsync(Guid reviewId, Guid reviewerId, CancellationToken ct = default);

    /// <summary>El revisor aprueba la entidad.</summary>
    Task ApproveAsync(Guid reviewId, Guid reviewerId, string? comments = null, CancellationToken ct = default);

    /// <summary>El revisor solicita cambios.</summary>
    Task RequestChangesAsync(Guid reviewId, Guid reviewerId, string comments, CancellationToken ct = default);

    /// <summary>Cancela la revisión.</summary>
    Task CancelAsync(Guid reviewId, Guid cancelledBy, CancellationToken ct = default);

    /// <summary>Obtiene una revisión por Id.</summary>
    Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken ct = default);

    /// <summary>Lista revisiones abiertas para una entidad específica.</summary>
    Task<IReadOnlyList<Review>> GetOpenReviewsForEntityAsync(
        Guid tenantId,
        Guid targetEntityId,
        CancellationToken ct = default);
}

using Evidata.Modules.ProcessingInventory.Application.ViewModels;
using Evidata.Modules.Workflow.Domain;

namespace Evidata.Modules.ProcessingInventory.Application.Helpers;

/// <summary>
/// Helper para mapear entre ReviewDomain (ProcessingInventory) y ReviewType (Workflow).
/// </summary>
public static class ReviewTypeMapper
{
    public static ReviewType ToReviewType(ReviewDomain domain) =>
        domain switch
        {
            ReviewDomain.Legal => ReviewType.Legal,
            ReviewDomain.Security => ReviewType.Security,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown ReviewDomain")
        };
}

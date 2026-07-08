using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Evidata.Modules.Identity.Infrastructure;

/// <summary>
/// Extensión para aplicar global query filter de tenant en cualquier DbContext.
/// Uso: modelBuilder.ApplyTenantFilter(currentUserContext)
/// </summary>
public static class TenantQueryFilterExtensions
{
    /// <summary>
    /// Aplica un global query filter a todas las entidades que implementen ITenantScoped,
    /// filtrando por el TenantId del usuario actual.
    /// </summary>
    public static void ApplyTenantFilter(this ModelBuilder modelBuilder, ICurrentUserContext currentUser)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
            var currentTenantId = Expression.Property(
                Expression.Constant(currentUser),
                nameof(ICurrentUserContext.TenantId));
            var filter = Expression.Lambda(Expression.Equal(tenantIdProperty, currentTenantId), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}

namespace Evidata.Modules.Security.Domain;

/// <summary>Asignación de un Role a un UserProfile dentro de un Tenant.</summary>
public class UserRoleAssignment
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    private UserRoleAssignment() { }

    public static UserRoleAssignment Create(Guid userId, Guid roleId, Guid tenantId)
    {
        return new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            TenantId = tenantId,
            AssignedAt = DateTime.UtcNow
        };
    }
}

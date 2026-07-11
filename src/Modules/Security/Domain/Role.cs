namespace Evidata.Modules.Security.Domain;

public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyList<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role() { }

    public static Role Create(string name, string? description = null, bool isSystemRole = false)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole
        };
    }

    /// <summary>
    /// Factory method for seeding with deterministic GUIDs (internal use only).
    /// This is used to ensure that seed data has consistent IDs across all EF Core model builds,
    /// preventing the "PendingModelChangesWarning" that occurs when HasData values change.
    /// </summary>
    internal static Role CreateForSeed(Guid id, string name, string? description = null, bool isSystemRole = false)
    {
        return new Role
        {
            Id = id,
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole
        };
    }

    public void AddPermission(Permission permission)
    {
        if (_permissions.Any(p => p.PermissionId == permission.Id)) return;
        _permissions.Add(new RolePermission(Id, permission.Id));
    }

    public void RemovePermission(Guid permissionId)
    {
        var rp = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (rp is not null) _permissions.Remove(rp);
    }
}

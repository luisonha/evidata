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

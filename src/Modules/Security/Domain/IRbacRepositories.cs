namespace Evidata.Modules.Security.Domain;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
}

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Permission?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Permission permission, CancellationToken ct = default);
}

public interface IUserRoleAssignmentRepository
{
    Task<IReadOnlyList<UserRoleAssignment>> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task AssignAsync(UserRoleAssignment assignment, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken ct = default);
}

using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Dummy implementation of IRoleNameResolver that returns "Unknown" for all queries.
/// Used in environments where the Security module is not available.
/// </summary>
public class NullRoleNameResolver : IRoleNameResolver
{
    public Task<string?> GetRoleNameAsync(Guid roleId, CancellationToken ct = default)
        => Task.FromResult<string?>("Unknown");

    public Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken ct = default)
        => Task.FromResult<Guid?>(null);

    public Task<IReadOnlyList<string>> GetRoleNamesAsync(IEnumerable<Guid> roleIds, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(
            roleIds.Select(_ => "Unknown").ToList().AsReadOnly());

    public Task<IReadOnlyList<Guid>> GetRoleIdsByNamesAsync(IEnumerable<string> roleNames, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>(new List<Guid>().AsReadOnly());
}

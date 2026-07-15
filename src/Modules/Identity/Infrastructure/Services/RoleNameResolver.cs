using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Evidata.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Resolves role IDs to names and vice versa by querying a DbContext that contains Role entities.
/// Uses reflection to work with any DbContext that has a Role entity type.
/// </summary>
public class RoleNameResolver : IRoleNameResolver
{
    private readonly DbContext _dbContext;
    private readonly IMemoryCache _cache;
    private Type? _roleType;

    public RoleNameResolver(DbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    private Type GetRoleType()
    {
        if (_roleType is not null)
            return _roleType;

        var entityTypes = _dbContext.Model.GetEntityTypes();
        _roleType = entityTypes.FirstOrDefault(e => e.ClrType.Name == "Role")?.ClrType
            ?? throw new InvalidOperationException("Could not find Role entity type in DbContext");
        return _roleType;
    }

    private IQueryable GetRolesQueryable()
    {
        var roleType = GetRoleType();
        var setMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == "AsNoTracking" && m.IsGenericMethodDefinition)
            .MakeGenericMethod(roleType);

        return (IQueryable)setMethod.Invoke(null, new object[] { GetDbSet(roleType) })!;
    }

    private IQueryable GetDbSet(Type roleType)
    {
        return (IQueryable)typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(roleType)
            .Invoke(_dbContext, null)!;
    }

    public async Task<string?> GetRoleNameAsync(Guid roleId, CancellationToken ct = default)
    {
        var cacheKey = $"role_name_{roleId}";
        if (_cache.TryGetValue(cacheKey, out string? cachedName))
        {
            return cachedName;
        }

        try
        {
            var roleType = GetRoleType();
            var idProperty = roleType.GetProperty("Id") ?? throw new InvalidOperationException("Role entity has no Id property");
            var nameProperty = roleType.GetProperty("Name") ?? throw new InvalidOperationException("Role entity has no Name property");

            var dbSet = GetDbSet(roleType);
            var roles = await dbSet.Cast<object>().ToListAsync(ct);

            var role = roles.FirstOrDefault(r => (Guid)idProperty.GetValue(r)! == roleId);
            if (role is null)
                return null;

            var name = (string?)nameProperty.GetValue(role);
            if (name is not null)
            {
                _cache.Set(cacheKey, name, TimeSpan.FromHours(1));
            }

            return name;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Guid?> GetRoleIdByNameAsync(string roleName, CancellationToken ct = default)
    {
        var cacheKey = $"role_id_{roleName}";
        if (_cache.TryGetValue(cacheKey, out Guid? cachedId) && cachedId.HasValue)
        {
            return cachedId;
        }

        try
        {
            var roleType = GetRoleType();
            var idProperty = roleType.GetProperty("Id") ?? throw new InvalidOperationException("Role entity has no Id property");
            var nameProperty = roleType.GetProperty("Name") ?? throw new InvalidOperationException("Role entity has no Name property");

            var dbSet = GetDbSet(roleType);
            var roles = await dbSet.Cast<object>().ToListAsync(ct);

            var role = roles.FirstOrDefault(r => (string?)nameProperty.GetValue(r) == roleName);
            if (role is null)
                return null;

            var id = (Guid?)idProperty.GetValue(role);
            if (id.HasValue)
            {
                _cache.Set(cacheKey, id, TimeSpan.FromHours(1));
            }

            return id;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(IEnumerable<Guid> roleIds, CancellationToken ct = default)
    {
        var ids = roleIds.Distinct().ToList();
        if (!ids.Any())
            return [];

        try
        {
            var roleType = GetRoleType();
            var idProperty = roleType.GetProperty("Id") ?? throw new InvalidOperationException("Role entity has no Id property");
            var nameProperty = roleType.GetProperty("Name") ?? throw new InvalidOperationException("Role entity has no Name property");

            var dbSet = GetDbSet(roleType);
            var roles = await dbSet.Cast<object>().ToListAsync(ct);

            var nameMap = new Dictionary<Guid, string>();
            foreach (var role in roles)
            {
                var id = (Guid)idProperty.GetValue(role)!;
                if (ids.Contains(id))
                {
                    var name = (string)nameProperty.GetValue(role)!;
                    nameMap[id] = name;
                }
            }

            return ids.Select(id => nameMap.TryGetValue(id, out var name) ? name : "Unknown")
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            return ids.Select(_ => "Unknown").ToList().AsReadOnly();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsByNamesAsync(IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        var names = roleNames.Distinct().ToList();
        if (!names.Any())
            return [];

        try
        {
            var roleType = GetRoleType();
            var idProperty = roleType.GetProperty("Id") ?? throw new InvalidOperationException("Role entity has no Id property");
            var nameProperty = roleType.GetProperty("Name") ?? throw new InvalidOperationException("Role entity has no Name property");

            var dbSet = GetDbSet(roleType);
            var roles = await dbSet.Cast<object>().ToListAsync(ct);

            var idMap = new Dictionary<string, Guid>();
            foreach (var role in roles)
            {
                var name = (string)nameProperty.GetValue(role)!;
                if (names.Contains(name))
                {
                    var id = (Guid)idProperty.GetValue(role)!;
                    idMap[name] = id;
                }
            }

            return names
                .Where(name => idMap.ContainsKey(name))
                .Select(name => idMap[name])
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            return [];
        }
    }
}


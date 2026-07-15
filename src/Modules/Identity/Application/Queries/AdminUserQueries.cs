using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Queries;

public record ListUsersQuery(Guid TenantId, string? SearchQuery = null, UserStatus? StatusFilter = null, string? RoleFilter = null, int Page = 1, int PageSize = 10);
public record GetUserDetailQuery(Guid TenantId, Guid UserId);

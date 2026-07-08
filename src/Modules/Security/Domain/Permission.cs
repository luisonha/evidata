namespace Evidata.Modules.Security.Domain;

public class Permission
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;      // e.g. "documents:read"
    public string Resource { get; private set; } = default!;  // e.g. "documents"
    public string Action { get; private set; } = default!;    // e.g. "read"
    public string? Description { get; private set; }

    private Permission() { }

    public static Permission Create(string resource, string action, string? description = null)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Resource = resource,
            Action = action,
            Name = $"{resource}:{action}",
            Description = description
        };
    }
}

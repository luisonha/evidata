using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Identity.Domain;

/// <summary>
/// Represents an invitation to a user to join a tenant.
/// </summary>
public class Invitation : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    
    /// <summary>
    /// The user profile associated with this invitation. Null until the invitation is accepted.
    /// </summary>
    public Guid? UserId { get; private set; }
    
    /// <summary>
    /// The email address to which the invitation was sent.
    /// </summary>
    public string Email { get; private set; } = default!;
    
    /// <summary>
    /// Comma-separated list of role names assigned to this invitation.
    /// Examples: "ProcessOwner", "LegalReviewer", "ProcessOwner,LegalReviewer"
    /// </summary>
    public string Roles { get; private set; } = default!;
    
    /// <summary>
    /// The responsible area/department for this invitation (optional).
    /// </summary>
    public Guid? ResponsibleAreaId { get; private set; }
    
    /// <summary>
    /// Status of the invitation.
    /// </summary>
    public InvitationStatus Status { get; private set; }
    
    /// <summary>
    /// The date and time when this invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }
    
    /// <summary>
    /// Optional message to include in the invitation.
    /// </summary>
    public string? Message { get; private set; }
    
    /// <summary>
    /// The user ID who created this invitation.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Invitation() { }

    /// <summary>
    /// Creates a new invitation.
    /// </summary>
    public static Invitation Create(
        Guid tenantId,
        string email,
        string roles,
        Guid createdByUserId,
        DateTime expiresAt,
        Guid? responsibleAreaId = null,
        string? message = null)
    {
        return new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = null,
            Email = email,
            Roles = roles,
            ResponsibleAreaId = responsibleAreaId,
            Status = InvitationStatus.Pending,
            ExpiresAt = expiresAt,
            Message = message,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Accepts the invitation and links it to a user profile.
    /// </summary>
    public void Accept(Guid userId)
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept an invitation with status {Status}");
        }

        if (DateTime.UtcNow > ExpiresAt)
        {
            throw new InvalidOperationException("Invitation has expired");
        }

        UserId = userId;
        Status = InvitationStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes the invitation.
    /// </summary>
    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot revoke an invitation with status {Status}");
        }

        Status = InvitationStatus.Revoked;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the invitation as expired.
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark as expired an invitation with status {Status}");
        }

        Status = InvitationStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the invitation is still valid (pending and not expired).
    /// </summary>
    public bool IsValid => Status == InvitationStatus.Pending && DateTime.UtcNow <= ExpiresAt;
}

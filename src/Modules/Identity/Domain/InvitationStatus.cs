namespace Evidata.Modules.Identity.Domain;

/// <summary>
/// Represents the status of an invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Invitation is pending acceptance.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Invitation has been accepted and the user is now active.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Invitation has been revoked by an administrator.
    /// </summary>
    Revoked = 2,

    /// <summary>
    /// Invitation has expired and can no longer be accepted.
    /// </summary>
    Expired = 3
}

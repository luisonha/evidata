namespace Evidata.Modules.Identity.Domain;

/// <summary>
/// Represents the lifecycle status of a user profile within a tenant.
/// 
/// State transitions:
/// - Invited -> Active (upon accepting invitation)
/// - Invited -> InvitationRevoked (admin revokes invitation before acceptance)
/// - Invited -> Expired (invitation expires without acceptance)
/// - Active -> Suspended (admin suspends user)
/// - Active -> Disabled (admin disables user)
/// - Suspended -> Active (admin reactivates user)
/// - Disabled -> Active (admin reactivates user)
/// - Suspended/Disabled -> InvitationRevoked (N/A - only for Invited state)
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User has been invited but has not yet accepted the invitation.
    /// </summary>
    Invited = 0,

    /// <summary>
    /// User has accepted the invitation and is active in the system.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User has been suspended (temporary deactivation).
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// User has been disabled (permanent deactivation during sprint 3).
    /// </summary>
    Disabled = 3,

    /// <summary>
    /// User's invitation has been revoked and cannot be re-accepted.
    /// </summary>
    InvitationRevoked = 4
}

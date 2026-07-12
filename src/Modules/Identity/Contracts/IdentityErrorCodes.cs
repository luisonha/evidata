namespace Evidata.Modules.Identity.Contracts;

/// <summary>
/// Catalog of error codes for the Identity module.
/// Each code is a semantic identifier that clients can use to provide specific feedback to users.
/// The HTTP status code varies depending on the operational context (see notes below).
///
/// All codes must be mapped in the error handling middleware to ApiErrorResponse.
/// </summary>
public static class IdentityErrorCodes
{
    // ──────────────────────────────────────────────────────────────────────────────────────
    // Authentication & Session Errors (HTTP 401)
    // ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// User is not authenticated (missing or invalid session/JWT).
    /// Context: Login flow, endpoints requiring authentication.
    /// HTTP Status: 401 Unauthorized
    /// </summary>
    public const string Unauthenticated = "Unauthenticated";

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Authorization & Access Control Errors (HTTP 403)
    // ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// User is authenticated but does not have the required permission for this operation.
    /// Context: Admin actions (manage users, change roles, etc.)
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string InsufficientPermissions = "InsufficientPermissions";

    /// <summary>
    /// CSRF (Cross-Site Request Forgery) token validation failed.
    /// Context: Form submissions from the web UI (login, invitations).
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string CsrfValidationFailed = "CsrfValidationFailed";

    /// <summary>
    /// Tenant is unavailable (suspended, deleted, or in an invalid state).
    /// Context: Any endpoint that validates tenant availability at request time.
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string TenantUnavailable = "TenantUnavailable";

    /// <summary>
    /// User attempting login is not provisioned for this tenant.
    /// Specifically: User exists in Entra ID but has no matching Invitation or UserProfile 
    /// in the Evidata tenant, and does not have an accepted invitation.
    /// Context: Login callback (/auth/callback).
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string UserNotProvisioned = "UserNotProvisioned";

    /// <summary>
    /// User is suspended and cannot perform this action.
    /// Context: Login, accessing protected endpoints.
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string UserSuspended = "UserSuspended";

    /// <summary>
    /// User is disabled and cannot perform this action.
    /// Context: Login, accessing protected endpoints.
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string UserDisabled = "UserDisabled";

    /// <summary>
    /// Invitation has been revoked and is no longer valid.
    /// 
    /// IMPORTANT: This code maps to TWO different HTTP statuses depending on context (Decision #1):
    /// - HTTP 403 (Forbidden) when user tries to use a revoked invitation (login context)
    /// - HTTP 409 (Conflict) when admin action detects the invitation is invalid (admin context)
    ///
    /// The semantic error (invitation revoked) is consistent, but the HTTP status reflects the operation type:
    /// 403 = access denied to a resource, 409 = incompatible state for the transaction.
    ///
    /// Context: Login callback with revoked invitation, admin actions involving invitations.
    /// HTTP Status: 403 (login/auth) or 409 (admin actions)
    /// </summary>
    public const string InvitationRevoked = "InvitationRevoked";

    /// <summary>
    /// Tenant ID mismatch: User's tenant does not match the invitation's tenant.
    /// Context: Login callback, cross-tenant validation.
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string TenantMismatch = "TenantMismatch";

    /// <summary>
    /// Email mismatch: User's email from identity provider does not match the invitation email.
    /// Context: Login callback validation.
    /// HTTP Status: 403 Forbidden
    /// </summary>
    public const string InvitationEmailMismatch = "InvitationEmailMismatch";

    /// <summary>
    /// Invitation has expired (expiresAt date has passed).
    /// Specific to expired invitations; distinct from InvitationRevoked for better client feedback.
    /// Context: Login callback, attempting to activate an expired invitation.
    /// HTTP Status: 403 Forbidden
    /// Reference: Decision #5 (Sprint 3)
    /// </summary>
    public const string InvitationExpired = "InvitationExpired";

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Conflict & State Errors (HTTP 409)
    // ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// User already exists in this tenant.
    /// Typically: Attempting to invite an email that already has an active user or pending invitation.
    /// Context: User invitation endpoint.
    /// HTTP Status: 409 Conflict
    /// </summary>
    public const string UserAlreadyExists = "UserAlreadyExists";

    /// <summary>
    /// Cannot perform this state transition: the requested operation violates business rules.
    /// Catch-all for invalid state transitions not explicitly covered by specific error codes.
    /// Examples: suspending the last TenantOwner, changing roles of a disabled user, etc.
    ///
    /// Note: When applicable, more specific codes should be used instead (e.g., LastTenantOwnerBlocked).
    /// This code is for edge cases and defensive validation.
    ///
    /// Context: Admin user state transitions (suspend, reactivate, disable, change roles).
    /// HTTP Status: 409 Conflict
    /// Reference: Decision #6 (Sprint 3)
    /// </summary>
    public const string InvalidStateTransition = "InvalidStateTransition";

    /// <summary>
    /// Cannot complete this operation: the last active TenantOwner would be removed or blocked.
    /// Context: Suspending, disabling, or removing the TenantOwner role from the last active owner.
    /// HTTP Status: 409 Conflict
    /// </summary>
    public const string LastTenantOwnerBlocked = "LastTenantOwnerBlocked";

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Validation Errors (HTTP 422 Unprocessable Entity)
    // ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Role provided is invalid or not assignable by the actor.
    /// Context: Inviting users with invalid roles, changing user roles to invalid values.
    /// HTTP Status: 422 Unprocessable Entity
    /// </summary>
    public const string InvalidRole = "InvalidRole";

    /// <summary>
    /// Responsible area (area de responsabilidad) provided is invalid or does not belong to the tenant.
    /// Note: ResponsibleAreaId represents a reference to a domain entity defined elsewhere (likely in GapManagement).
    /// Validation must confirm it exists and belongs to the current tenant.
    /// Context: User invitation with invalid responsibleAreaId.
    /// HTTP Status: 422 Unprocessable Entity
    /// </summary>
    public const string InvalidResponsibleArea = "InvalidResponsibleArea";

    /// <summary>
    /// Required reason field is missing.
    /// Context: Admin operations that require explicit justification (suspend, disable, change role).
    /// HTTP Status: 422 Unprocessable Entity
    /// </summary>
    public const string ReasonRequired = "ReasonRequired";

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Resource Not Found Errors (HTTP 404)
    // ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// User not found in this tenant.
    /// Context: Get/update user endpoints, admin operations targeting a specific user.
    /// HTTP Status: 404 Not Found
    /// </summary>
    public const string UserNotFound = "UserNotFound";

    // ──────────────────────────────────────────────────────────────────────────────────────
    // HTTP Status to Error Code Mapping Reference
    // ──────────────────────────────────────────────────────────────────────────────────────
    // HTTP 401: Unauthenticated
    // HTTP 403: InsufficientPermissions, CsrfValidationFailed, TenantUnavailable, 
    //           UserNotProvisioned, UserSuspended, UserDisabled, InvitationRevoked (login only),
    //           TenantMismatch, InvitationEmailMismatch, InvitationExpired
    // HTTP 404: UserNotFound
    // HTTP 409: UserAlreadyExists, InvalidStateTransition, LastTenantOwnerBlocked, 
    //           InvitationRevoked (admin context only)
    // HTTP 422: InvalidRole, InvalidResponsibleArea, ReasonRequired
}

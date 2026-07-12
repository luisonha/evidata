namespace Evidata.Modules.Identity.Domain;

/// <summary>
/// Base exception for domain-level errors in the Identity module.
/// Encapsulates an error code from IdentityErrorCodes and an optional user-facing message.
/// 
/// This exception is designed to be caught and mapped to ApiErrorResponse by the error handling middleware.
/// The HTTP status code is determined by the error code and operational context.
/// </summary>
public class IdentityDomainException : Exception
{
    /// <summary>
    /// The error code from IdentityErrorCodes catalog.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Optional user-facing message (distinct from technical exception message).
    /// If null, the error handling middleware should use ErrorCode to look up a localized message.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Optional details dictionary for the ApiErrorResponse.
    /// </summary>
    public Dictionary<string, object?>? Details { get; }

    /// <summary>
    /// Initializes a new instance of IdentityDomainException with an error code.
    /// </summary>
    /// <param name="errorCode">The error code from IdentityErrorCodes.</param>
    /// <param name="message">Technical message for logging (Exception.Message).</param>
    public IdentityDomainException(string errorCode, string? message = null)
        : base(message ?? errorCode)
    {
        ErrorCode = errorCode;
        ErrorMessage = null;
        Details = null;
    }

    /// <summary>
    /// Initializes a new instance of IdentityDomainException with code, user message, and details.
    /// </summary>
    /// <param name="errorCode">The error code from IdentityErrorCodes.</param>
    /// <param name="errorMessage">Optional user-facing error message.</param>
    /// <param name="message">Technical message for logging (Exception.Message).</param>
    /// <param name="details">Optional details dictionary for the ApiErrorResponse.</param>
    public IdentityDomainException(
        string errorCode,
        string? errorMessage,
        string? message = null,
        Dictionary<string, object?>? details = null)
        : base(message ?? errorMessage ?? errorCode)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Details = details;
    }

    /// <summary>
    /// Initializes a new instance of IdentityDomainException with an inner exception.
    /// </summary>
    /// <param name="errorCode">The error code from IdentityErrorCodes.</param>
    /// <param name="message">Technical message for logging (Exception.Message).</param>
    /// <param name="innerException">The inner exception.</param>
    public IdentityDomainException(string errorCode, string? message, Exception innerException)
        : base(message ?? errorCode, innerException)
    {
        ErrorCode = errorCode;
        ErrorMessage = null;
        Details = null;
    }
}

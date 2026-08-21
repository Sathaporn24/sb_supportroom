namespace SupportRoom.Domain.Enums;

/// <summary>
/// String constants, not an enum - same reasoning as SessionStatus/AnswerStatus. Must stay in
/// step with the ApiErrorCode union in src/types/api.ts.
///
/// UNAUTHORIZED and FORBIDDEN are deliberately distinct and must not be collapsed into one:
/// UNAUTHORIZED (401) means "we don't know who you are" and the frontend answers it by sending
/// the user to sign in; FORBIDDEN (403) means "we know exactly who you are and the answer is no",
/// where bouncing to a login screen would be a confusing dead end - the user is already signed in
/// and signing in again changes nothing.
/// </summary>
public static class ApiErrorCode
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string UpstreamError = "UPSTREAM_ERROR";
    public const string ConfigError = "CONFIG_ERROR";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";
}

namespace SupportRoom.Domain.Enums;

/// <summary>
/// Only 4 of these are actually emitted today (VALIDATION_ERROR, NOT_FOUND, UPSTREAM_ERROR,
/// CONFIG_ERROR) - Unauthorized and InternalError are reserved-but-unused in the TS source
/// (src/types/api.ts), kept here as the natural hook points for auth and the global exception
/// handler. String constants, not an enum - same reasoning as SessionStatus/AnswerStatus.
/// </summary>
public static class ApiErrorCode
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string UpstreamError = "UPSTREAM_ERROR";
    public const string ConfigError = "CONFIG_ERROR";
    public const string InternalError = "INTERNAL_ERROR";
}

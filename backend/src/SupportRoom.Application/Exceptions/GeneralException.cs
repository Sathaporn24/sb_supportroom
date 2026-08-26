using System.Net;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Application.Exceptions;

/// <summary>
/// Static factory of every error case a Service can throw (see .claude/skills/
/// dotnet-layered-backend/SKILL.md §"Exception handling") - new error cases become new
/// static methods here, never a bare throw new Exception(...)/string.
///
/// Deliberately single-message (Thai only), keyed by the *existing* SupportRoom.Domain.Enums.
/// ApiErrorCode values - NOT the reference project's ERR-00XX numeric codes / EN+TH pair.
/// ApiErrorBody (SupportRoom.Domain) has always had one Message field, and the frontend this
/// API mirrors (src/lib/api-response.ts) switches on these exact code strings - changing
/// either would be a wire-breaking change with no consumer asking for it.
/// </summary>
public static class GeneralException
{
    public static HttpStatusCodeException NotFound(string entityNameTh)
        => new(HttpStatusCode.NotFound, ApiErrorCode.NotFound, $"ไม่พบ{entityNameTh}");

    public static HttpStatusCodeException ValidationError(string messageTh)
        => new(HttpStatusCode.BadRequest, ApiErrorCode.ValidationError, messageTh);

    public static HttpStatusCodeException Unauthorized(string messageTh)
        => new(HttpStatusCode.Unauthorized, ApiErrorCode.Unauthorized, messageTh);

    /// <summary>Signed in, but not allowed. Distinct from Unauthorized so the frontend can tell
    /// "go log in" apart from "logging in again won't help" - see ApiErrorCode.</summary>
    public static HttpStatusCodeException Forbidden(string messageTh)
        => new(HttpStatusCode.Forbidden, ApiErrorCode.Forbidden, messageTh);

    /// <summary>KL-21 - the request is well-formed but conflicts with existing data (duplicate
    /// content/name on upload). <paramref name="details"/> carries the structured payload the
    /// caller needs to act on it (DuplicateDocumentDto) - unlike every other factory here, this one
    /// is meant to be read programmatically by the frontend, not just displayed as messageTh.</summary>
    public static HttpStatusCodeException Conflict(string messageTh, object? details = null)
        => new(HttpStatusCode.Conflict, ApiErrorCode.Conflict, messageTh, details);

    public static HttpStatusCodeException UpstreamError(string messageTh)
        => new(HttpStatusCode.BadGateway, ApiErrorCode.UpstreamError, messageTh);

    public static HttpStatusCodeException ConfigError(string messageTh)
        => new(HttpStatusCode.InternalServerError, ApiErrorCode.ConfigError, messageTh);
}

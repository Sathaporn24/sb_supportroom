using System.Net;

namespace SupportRoom.Application.Exceptions;

/// <summary>
/// No built-in ASP.NET Core equivalent - carries the same {code, message} the existing
/// ApiErrorEnvelope/ApiErrorBody (SupportRoom.Domain) already writes to the response, plus
/// the HTTP status to reply with. GeneralException is the only place that should construct
/// this (see .claude/skills/dotnet-layered-backend/SKILL.md).
/// </summary>
public class HttpStatusCodeException(HttpStatusCode statusCode, string code, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}

using Microsoft.AspNetCore.Http;
using SupportRoom.Domain;

namespace SupportRoom.Infrastructure.ErrorHandling;

/// <summary>
/// Mirrors src/lib/api-response.ts's jsonError() / src/types/api.ts's apiError(): builds the
/// {error:{code,message,details?,requestId}} envelope with a fresh per-request id, and writes
/// it to the response. Controllers should return this via IActionResult (see AsActionResult),
/// the exception handler writes it directly onto HttpContext.Response for uncaught errors.
/// </summary>
public static class ApiErrorEnvelope
{
    public static ApiErrorResponse Build(string code, string message, object? details = null)
    {
        return new ApiErrorResponse
        {
            Error = new ApiErrorBody
            {
                Code = code,
                Message = message,
                Details = details,
                RequestId = IdGenerator.GenerateId("req"),
            },
        };
    }

    public static async Task WriteAsync(HttpContext context, int statusCode, string code, string message, object? details = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(Build(code, message, details), ApiJsonOptions.Default);
    }
}

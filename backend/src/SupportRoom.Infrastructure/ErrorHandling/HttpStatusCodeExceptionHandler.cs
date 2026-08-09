using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SupportRoom.Application.Exceptions;

namespace SupportRoom.Infrastructure.ErrorHandling;

/// <summary>
/// Turns a GeneralException-produced HttpStatusCodeException into the same {error:{code,
/// message,...}} envelope every other error path uses. Registered ahead of
/// GlobalExceptionHandler in Program.cs so this runs first and the generic 500 fallback only
/// catches truly unexpected exceptions.
/// </summary>
public sealed class HttpStatusCodeExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not HttpStatusCodeException statusCodeException)
        {
            return false;
        }

        await ApiErrorEnvelope.WriteAsync(httpContext, (int)statusCodeException.StatusCode, statusCodeException.Code, statusCodeException.Message);
        return true;
    }
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Infrastructure.ErrorHandling;

/// <summary>
/// Catches anything not explicitly handled by a controller and maps it to the same envelope
/// shape as every other error path, using the INTERNAL_ERROR code that src/types/api.ts
/// declared but never emitted (Next.js's implicit 500 didn't match this envelope at all -
/// this is stricter/better behavior, not a contract change, since the code was already
/// reserved for exactly this).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception");
        await ApiErrorEnvelope.WriteAsync(httpContext, StatusCodes.Status500InternalServerError, ApiErrorCode.InternalError, "เกิดข้อผิดพลาดที่ไม่คาดคิด");
        return true;
    }
}

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Concurrent;
using SupportRoom.Domain.Enums;
using SupportRoom.Infrastructure.ErrorHandling;

namespace SupportRoom.Api.Configurations;

/// <summary>Brute-force protection for the only anonymous back-office endpoint. The partition
/// comes from RemoteIpAddress after ASP.NET Core's conservative forwarded-header processing;
/// untrusted clients cannot forge it with X-Forwarded-For.</summary>
public static class LoginRateLimitConfiguration
{
    public const string LoginPolicyName = "admin-login";
    public const int PermitLimit = 10;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddSingleton<ILoginAccountRateLimiter, LoginAccountRateLimiter>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (rejection, _) => WriteRejectedResponseAsync(rejection.HttpContext);
            options.AddPolicy(LoginPolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PermitLimit,
                        Window = Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        });

        return services;
    }

    public static ValueTask WriteRejectedResponseAsync(HttpContext context)
        => new(ApiErrorEnvelope.WriteAsync(
            context,
            StatusCodes.Status429TooManyRequests,
            ApiErrorCode.RateLimited,
            "มีความพยายามเข้าสู่ระบบมากเกินไป กรุณาลองใหม่ภายหลัง"));
}

/// <summary>Second, short-lived partition for a normalized account identifier. It complements
/// the ASP.NET Core source-IP policy and cannot reveal whether that identifier has an account:
/// every request beyond the window receives the same 429 envelope.</summary>
public interface ILoginAccountRateLimiter
{
    bool TryAcquire(string email);
}

public sealed class LoginAccountRateLimiter : ILoginAccountRateLimiter
{
    public const int PermitLimit = 20;
    public const int MaximumTrackedAccounts = 10_000;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, AttemptWindow> _attempts = new(StringComparer.Ordinal);

    public bool TryAcquire(string email)
    {
        var now = DateTime.UtcNow;
        RemoveExpiredEntries(now);
        var normalizedEmail = email.Trim().ToUpperInvariant();

        if (!_attempts.TryGetValue(normalizedEmail, out var window))
        {
            if (_attempts.Count >= MaximumTrackedAccounts)
            {
                return false;
            }

            window = _attempts.GetOrAdd(normalizedEmail, _ => new AttemptWindow(now + Window));
        }

        lock (window)
        {
            if (now >= window.ExpiresAt)
            {
                window.ExpiresAt = now + Window;
                window.Count = 0;
            }

            if (window.Count >= PermitLimit)
            {
                return false;
            }

            window.Count++;
            return true;
        }
    }

    private void RemoveExpiredEntries(DateTime now)
    {
        foreach (var entry in _attempts)
        {
            if (now >= entry.Value.ExpiresAt)
            {
                _attempts.TryRemove(entry.Key, out _);
            }
        }
    }

    private sealed class AttemptWindow(DateTime expiresAt)
    {
        public DateTime ExpiresAt { get; set; } = expiresAt;
        public int Count { get; set; }
    }
}

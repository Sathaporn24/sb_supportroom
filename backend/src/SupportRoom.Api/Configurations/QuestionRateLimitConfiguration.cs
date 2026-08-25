using System.Collections.Concurrent;

namespace SupportRoom.Api.Configurations;

/// <summary>SEC-02 - brute-force/cost-abuse protection for the two anonymous question endpoints
/// (`/api/text-question`, `/api/voice-question`). Each question is expensive (embedding + up to
/// three Pinecone namespace queries + an LLM answer call, plus TTS on the voice path), and both
/// endpoints must stay [AllowAnonymous] because learners have no account.
///
/// Partitioned by learnerKey, not RemoteIpAddress like LoginRateLimitConfiguration: a whole
/// classroom of learners can sit behind one NAT'd IP on the same link, and an IP-keyed limiter
/// would let one learner's spam lock out everyone else in the room. learnerKey is the per-learner
/// identity this system already has for anonymous learners (see AskVoiceQuestionDto.LearnerKey);
/// scoping the key to token+learnerKey together (not learnerKey alone) keeps two different links'
/// windows independent, same reasoning as LoginAccountRateLimiter normalizing by account rather
/// than by source alone.
///
/// A plain service (TryAcquire, checked inside the controller action) rather than
/// [EnableRateLimiting] + AddPolicy: the built-in policy's partition-key resolver runs before MVC
/// model binding, so it cannot read learnerKey out of a JSON body or a multipart form field
/// without hand-parsing the request twice. Mirrors ILoginAccountRateLimiter exactly.</summary>
public interface IQuestionRateLimiter
{
    bool TryAcquire(string token, string learnerKey);
}

public sealed class QuestionRateLimiter : IQuestionRateLimiter
{
    public const int PermitLimit = 10;
    public const int MaximumTrackedKeys = 50_000;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, AttemptWindow> _attempts = new(StringComparer.Ordinal);

    public bool TryAcquire(string token, string learnerKey)
    {
        var now = DateTime.UtcNow;
        RemoveExpiredEntries(now);
        var key = $"{token.Trim()}:{learnerKey.Trim()}";

        if (!_attempts.TryGetValue(key, out var window))
        {
            if (_attempts.Count >= MaximumTrackedKeys)
            {
                return false;
            }

            window = _attempts.GetOrAdd(key, _ => new AttemptWindow(now + Window));
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

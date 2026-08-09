using System.Text.Json;

namespace SupportRoom.Infrastructure.ErrorHandling;

/// <summary>
/// Single source of truth for JSON casing across the app. The frontend (src/lib/api-client.ts)
/// expects exact camelCase field names (lessonSlug, presentationId, unansweredPoints, ...) with
/// zero changes - Program.cs's AddJsonOptions must use this same policy, and any place that
/// writes JSON outside the normal MVC pipeline (e.g. ApiErrorEnvelope below) must too.
/// </summary>
public static class ApiJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

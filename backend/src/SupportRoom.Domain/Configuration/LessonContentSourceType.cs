namespace SupportRoom.Domain.Configuration;

/// <summary>
/// Per-lesson choice of where teaching content comes from - unlike ProviderSelection's
/// env-var switches (one value for the whole deployment), this is a per-LessonConfig field
/// CS picks per lesson, so it lives on the entity/DTO rather than being read from env.
/// </summary>
public static class LessonContentSourceType
{
    public const string GoogleSlides = "google_slides";
    public const string Pdf = "pdf";
    public static readonly string[] Allowed = [GoogleSlides, Pdf];
}

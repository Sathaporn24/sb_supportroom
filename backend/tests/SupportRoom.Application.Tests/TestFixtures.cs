namespace SupportRoom.Application.Tests;

/// <summary>
/// Real, stable, publicly-viewable resources used as fixtures now that tests call Real providers
/// instead of Mock ones - a Mock's canned deck/presentation id doesn't exist anymore, so
/// Slides-related tests need something real to point the real GoogleSlidesProvider at.
/// </summary>
internal static class TestFixtures
{
    /// <summary>Google's own official Slides API quickstart sample ("Baby album", 5 slides,
    /// publicly viewable) - verified reachable with this project's service account credentials.
    /// Source: https://developers.google.com/slides/api/quickstart (SAMPLE_PRESENTATION_ID).</summary>
    public const string GooglePresentationId = "1EAYk18WDjIG-zp_0vLm3CsfQh_i8eXc67Jo2O9C6Vuc";

    // Named TestGoogleSlidesUrl, not GoogleSlidesUrl - that name collides with
    // SupportRoom.Providers.Slides.GoogleSlidesUrl (a real class, not a test fixture).
    public const string TestGoogleSlidesUrl = $"https://docs.google.com/presentation/d/{GooglePresentationId}/edit";
}

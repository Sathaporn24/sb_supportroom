namespace SupportRoom.Application.Tests;

/// <summary>
/// Mock providers were deliberately removed from this codebase, so a test that exercises a
/// provider exercises the REAL one - Google Slides, Gemini, Pinecone, Edge TTS. Those need the
/// gitignored .env and a working network, which CI has neither of (see TD-006).
///
/// Tagging them keeps the default run honest: `dotnet test --filter Category!=Integration` is
/// all green, so a red result means something actually broke instead of "this machine has no
/// credentials" - which is exactly the signal that gets lost when 10 tests are permanently red.
///
/// const string rather than a literal at each call site: a typo'd trait name silently stops
/// filtering the test, and nothing fails to tell you.
/// </summary>
public static class TestCategories
{
    public const string Category = "Category";

    /// <summary>Needs real credentials and/or network. Excluded from the default CI run.</summary>
    public const string Integration = "Integration";
}

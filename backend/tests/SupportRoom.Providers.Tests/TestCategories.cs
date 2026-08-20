namespace SupportRoom.Providers.Tests;

/// <summary>
/// Same purpose and reasoning as the copy in SupportRoom.Application.Tests - duplicated rather
/// than shared for the same reason TestEnv.cs and RealHttpClientFactory.cs already are: the two
/// test projects don't reference each other, and a shared test-support project would be more
/// moving parts than the handful of lines it would save.
/// </summary>
public static class TestCategories
{
    public const string Category = "Category";

    /// <summary>Needs real credentials and/or network. Excluded from the default CI run.</summary>
    public const string Integration = "Integration";
}

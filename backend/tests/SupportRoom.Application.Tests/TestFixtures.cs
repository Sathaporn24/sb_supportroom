using SupportRoom.Application.Common;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Application.Tests;

/// <summary>
/// Real, stable, publicly-viewable resources used as fixtures now that tests call Real providers
/// instead of Mock ones - a Mock's canned deck/presentation id doesn't exist anymore, so
/// Slides-related tests need something real to point the real GoogleSlidesProvider at.
/// </summary>
internal static class TestFixtures
{
    /// <summary>
    /// Builds the REAL AuthorizationGuard over a resolved CurrentUser rather than a fake that
    /// always says yes. The rules this guard encodes are the only thing protecting Company and
    /// AdminUser (neither has a company query filter), so a test that stubs them out would prove
    /// nothing about the code that actually ships.
    /// </summary>
    public static IAuthorizationGuard GuardFor(string role, string? companyId, string userId = "user-test")
    {
        var user = new CurrentUser();
        user.Resolve(userId, role, companyId);
        return new AuthorizationGuard(user);
    }

    public static IAuthorizationGuard OwnerGuard(string userId = "user-owner")
        => GuardFor(AdminRole.Owner, companyId: null, userId);

    /// <summary>A guard for a request nobody is signed in to - every check must refuse.</summary>
    public static IAuthorizationGuard AnonymousGuard() => new AuthorizationGuard(new CurrentUser());

    /// <summary>Company every seeded entity belongs to. FakeServiceProvider pre-resolves
    /// ICompanyContext to this value, so services under test stamp the same id on rows they
    /// create and the two match up.</summary>
    public const string CompanyId = "company-test";

    /// <summary>A second company, for tests that assert one company cannot see another's rows.</summary>
    public const string OtherCompanyId = "company-other";

    /// <summary>Google's own official Slides API quickstart sample ("Baby album", 5 slides,
    /// publicly viewable) - verified reachable with this project's service account credentials.
    /// Source: https://developers.google.com/slides/api/quickstart (SAMPLE_PRESENTATION_ID).</summary>
    public const string GooglePresentationId = "1EAYk18WDjIG-zp_0vLm3CsfQh_i8eXc67Jo2O9C6Vuc";

    // Named TestGoogleSlidesUrl, not GoogleSlidesUrl - that name collides with
    // SupportRoom.Providers.Slides.GoogleSlidesUrl (a real class, not a test fixture).
    public const string TestGoogleSlidesUrl = $"https://docs.google.com/presentation/d/{GooglePresentationId}/edit";
}

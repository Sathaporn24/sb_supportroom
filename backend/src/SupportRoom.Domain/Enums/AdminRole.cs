namespace SupportRoom.Domain.Enums;

/// <summary>
/// What an AdminUser may do. String constants for the same reason as SessionStatus - the TS union
/// type serializes exactly these values.
///
/// | role  | scope            | may do                                                        |
/// |-------|------------------|---------------------------------------------------------------|
/// | owner | every company    | everything + manage the company list + API key/provider settings|
/// | admin | own company only | all of that company's work + add/deactivate its own people     |
/// | cs    | own company only | create links, edit lessons, upload documents, review answers   |
///
/// The person who receives a training link is NOT in this list. They have no account: their token
/// is both key and scope. Do not add a role for them.
/// </summary>
public static class AdminRole
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Cs = "cs";

    /// <summary>
    /// Rank used for one rule only: you may never create or promote someone to a role above your
    /// own. Without it, a customer's `admin` could promote themselves to `owner` through the very
    /// user-management feature we built for them and read every other customer's data - passing
    /// every check we wrote, because managing users is genuinely their job.
    ///
    /// Deliberately not exposed as an ordering for anything else: "higher rank" is not a general
    /// permission model, it is the answer to this single escalation question.
    /// </summary>
    private static int RankOf(string role) => role switch
    {
        Owner => 3,
        Admin => 2,
        Cs => 1,
        _ => 0,
    };

    public static bool IsValid(string? role)
        => role is Owner or Admin or Cs;

    /// <summary>True when an actor holding <paramref name="actorRole"/> may assign
    /// <paramref name="targetRole"/>. Unknown roles rank 0 and can assign nothing.</summary>
    public static bool CanAssign(string actorRole, string targetRole)
        => IsValid(targetRole) && RankOf(actorRole) >= RankOf(targetRole);

    /// <summary>Owner is the only role that spans companies, and the only one whose CompanyId
    /// may be null.</summary>
    public static bool IsCompanyScoped(string role) => role is Admin or Cs;
}

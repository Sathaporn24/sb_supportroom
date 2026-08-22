using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// A customer that operates its own support room - School Bright itself, SCB, and so on.
/// School Bright is a row here too: it owns the system AND uses it for its own teachers.
///
/// ⚠️ This entity is deliberately NOT ICompanyScoped and has NO query filter. It is the tenant
/// registry itself: filtering it by the current company would make the company switcher unable
/// to list anything to switch to. Every other table gets automatic protection from the filter -
/// this one does not, so each query against it must scope itself explicitly and is covered by
/// tests rather than by the filter (see TD-014).
/// </summary>
public sealed class Company : IEntityMaster<string>
{
    /// <summary>
    /// Slug, not a generated id - it appears in admin URLs as ?company=scb, which means it lands
    /// in browser history, server logs and referrer headers. Must therefore never carry anything
    /// confidential: no contract numbers, no registration numbers. Validated by CompanySlug.IsValid
    /// on the way in.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Display name shown in the switcher, e.g. "ธนาคารไทยพาณิชย์".</summary>
    public required string Name { get; set; }

    /// <summary>Inactive companies disappear from the switcher but their data is untouched -
    /// deactivating is how a customer is offboarded without destroying history.</summary>
    public required bool IsActive { get; set; }

    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>Default lesson pacing for every lesson in this company (Module P). Must never be
    /// null (LP-1/LP-2/P5) - set from ServerDefaults.GetLessonTimingDefaults() at the two places a
    /// Company row is created (ICompanyService.Create / SeedFirstCompanyIfEmpty) and never
    /// anywhere else. This is the last layer of the pacing resolve chain - there is no further
    /// fallback to environment variables at read time, unlike the settings Module B will add
    /// later, where null means "inherit". Do not add a query filter alongside these fields - see
    /// the class-level note above; that constraint predates Module P and still applies.</summary>
    public required int DefaultIntroWaitMs { get; set; }
    public required int DefaultBreathPauseMs { get; set; }
    public required int DefaultFinalQuestionWaitMs { get; set; }
}

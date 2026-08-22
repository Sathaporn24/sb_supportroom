namespace SupportRoom.Application.ViewModel;

/// <summary>One entry in the company switcher. Mirrors Company in domain.ts.</summary>
public sealed class CompanyViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>Response for GET/PUT /api/companies/{companyId}/lesson-pacing (LP-9). Always NOT NULL
/// - the company layer is the last layer of the pacing resolve chain (LP-1), so there is no
/// further fallback to represent as "unset".</summary>
public sealed class CompanyLessonPacingViewModel
{
    public required int IntroWaitMs { get; init; }
    public required int BreathPauseMs { get; init; }
    public required int FinalQuestionWaitMs { get; init; }
}

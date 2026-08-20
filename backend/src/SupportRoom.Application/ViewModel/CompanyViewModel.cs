namespace SupportRoom.Application.ViewModel;

/// <summary>One entry in the company switcher. Mirrors Company in domain.ts.</summary>
public sealed class CompanyViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}

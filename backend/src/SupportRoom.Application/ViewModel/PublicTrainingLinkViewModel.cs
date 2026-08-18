namespace SupportRoom.Application.ViewModel;

/// <summary>Minimum link metadata needed before an anonymous learner joins.</summary>
public sealed class PublicTrainingLinkViewModel
{
    public required string Token { get; init; }
    public string? RecipientOrgName { get; init; }
    public required string ExpiresAt { get; init; }
    public required string Status { get; init; }
}

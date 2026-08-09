using SupportRoom.Domain;

namespace SupportRoom.Providers.Slides;

public sealed class ResolvePresentationInput
{
    public required string SlidesSourceUrl { get; init; }
    public string? SlidesEmbedUrl { get; init; }
}

public sealed class GetLessonContentInput
{
    public required string PresentationId { get; init; }
}

public interface ISlidesProvider
{
    Task<ResolvedPresentation> ResolvePresentationAsync(ResolvePresentationInput input);
    Task<SlidesLessonContent> GetLessonContentAsync(GetLessonContentInput input);
}

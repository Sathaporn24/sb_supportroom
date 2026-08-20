using SupportRoom.Domain;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

/// <summary>
/// NR-1's per-page narration resolver: a LessonSlideNarration row for (LessonId, SlideObjectId)
/// wins, otherwise the page's freshly-extracted SpeakerNotes stands. This is the ONE place that
/// merge decision is made - both ILessonConfigService (what the tutor engine speaks, and what
/// NR-7 indexes on a full lesson save) and IBackgroundJobProcessor's lesson_index handler (NR-6's
/// partial re-index) call this same resolver, so what CS hears in class and what the RAG answers
/// from can never drift apart the way they did before this table existed.
/// </summary>
public interface ILessonSlideNarrationResolver
{
    Task<IReadOnlyList<ResolvedSlide>> ResolveAsync(string lessonId, IReadOnlyList<ResolvedSlide> baseSlides);
}

public sealed class LessonSlideNarrationResolver(IUnitOfWork unitOfWork) : ILessonSlideNarrationResolver
{
    private readonly ILessonSlideNarrationRepository _repository = unitOfWork.GetRepository<ILessonSlideNarrationRepository>();

    public Task<IReadOnlyList<ResolvedSlide>> ResolveAsync(string lessonId, IReadOnlyList<ResolvedSlide> baseSlides)
    {
        var overrides = _repository.GetByLessonId(lessonId).ToDictionary(x => x.SlideObjectId, x => x.NarrationText);
        if (overrides.Count == 0)
        {
            return Task.FromResult(baseSlides);
        }

        var resolved = baseSlides
            .Select(slide => overrides.TryGetValue(slide.SlideObjectId, out var narrationText)
                ? new ResolvedSlide
                {
                    SlideObjectId = slide.SlideObjectId,
                    Index = slide.Index,
                    SpeakerNotes = narrationText,
                    SlideUrl = slide.SlideUrl,
                }
                : slide)
            .ToList();

        return Task.FromResult<IReadOnlyList<ResolvedSlide>>(resolved);
    }
}

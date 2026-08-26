using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

/// <summary>P11-01 - "หน้าละหนึ่งแถว" (EX-4/DM-17) must hold after every write path that touches a
/// lesson's exclusion state, not just LessonConfigService.SaveAsync's full-lesson save. Loads every
/// LessonExcludedSlide row for the lesson (soft-deleted included), hard-deletes every row in a
/// (LessonId, SlideObjectId) group beyond one representative - same tie-break used everywhere else
/// in this feature: prefer a live row, else the most recently touched - and returns the surviving
/// representative rows keyed by SlideObjectId. Both ILessonConfigService.ApplyExcludedSlidesAsync
/// and ILessonExcludedSlideService.ToggleAsync call this before doing anything else, so a legacy
/// duplicate left by the bug that predates this invariant being enforced gets collapsed regardless
/// of which entry point is used.</summary>
internal static class LessonExcludedSlideReconciler
{
    public static Dictionary<string, LessonExcludedSlide> ReconcileAndLoad(
        ILessonExcludedSlideRepository repository, string lessonId)
    {
        var groupedBySlideObjectId = repository.GetByLessonId(lessonId)
            .ToList()
            .GroupBy(x => x.SlideObjectId)
            .ToList();

        var result = new Dictionary<string, LessonExcludedSlide>();
        foreach (var group in groupedBySlideObjectId)
        {
            var ordered = group.OrderBy(x => x.IsDelete).ThenByDescending(x => x.CreateDate).ToList();
            foreach (var duplicate in ordered.Skip(1))
            {
                repository.Delete(duplicate);
            }
            result[group.Key] = ordered[0];
        }
        return result;
    }
}

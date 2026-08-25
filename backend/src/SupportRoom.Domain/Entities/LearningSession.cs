using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// One person's run through one lesson. Many of these hang off a single TrainingLink - the link
/// is handed to a whole department and each person who opens it learns 1:1 (a classroom, not a
/// meeting room), so progress belongs here and never on the link.
///
/// This is what "session" means everywhere else in the codebase: SessionQuestion.SessionId points
/// at this table, not at TrainingLink.
/// </summary>
public sealed class LearningSession : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string TrainingLinkId { get; init; }

    /// <summary>
    /// Opaque key the browser generates and keeps in local storage. Does two jobs at once
    /// (CORE_FEATURE_SPEC §2.4): it lets someone resume after a dropped connection or a closed
    /// tab, and it tells two people apart on the same link - the second person to open a link
    /// must never land in the first person's session.
    ///
    /// Not an identity: together with the TrainingLink token this is the composite bearer
    /// credential for one learner's data. Neither half is sufficient on its own.
    /// </summary>
    public required string LearnerKey { get; init; }

    /// <summary>Display label the learner typed on the join screen - not an identity, not
    /// authentication. Duplicates across a link are expected and fine; LearnerKey is what
    /// separates people.</summary>
    public required string RecipientName { get; init; }

    /// <summary>IN_PROGRESS | ENDED. "Stalled" is deliberately NOT stored here - it is derived
    /// from LastActivityAt at read time (see ILearningSessionService), because the browser signal
    /// that would set it is missed exactly when it matters most (closed laptop, dead battery,
    /// lost connection).</summary>
    public required string Status { get; set; }

    public required DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; set; }

    /// <summary>Bumped on every slide change. The only input to the stalled calculation.</summary>
    public required DateTime LastActivityAt { get; set; }

    public string? LastSlideObjectId { get; set; }

    /// <summary>Kept alongside LastSlideObjectId purely so "7/20" can be rendered without
    /// re-resolving the whole deck from Google Slides just to find the index.
    ///
    /// Nullable because 0 is a real slide: a non-nullable int made "never sent a progress ping"
    /// indistinguishable from "sitting on the first slide", and any request that arrived before
    /// the deck had loaded silently overwrote real progress with 0.</summary>
    public int? LastSlideIndex { get; set; }

    /// <summary>How many slides the deck had while THIS person was learning. Stored here rather
    /// than on the link because the deck can be edited mid-flight and the count has to match what
    /// this learner actually saw. Without it "slide 7" cannot be rendered as "7/20", and there is
    /// no way to tell that someone who closed the tab had in fact reached the end.</summary>
    public int? TotalSlideCount { get; set; }

    public bool CompletedAllSlides { get; set; }
}

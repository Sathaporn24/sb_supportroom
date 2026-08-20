namespace SupportRoom.Application.ViewModel;

/// <summary>
/// What the join screen needs to decide which of six screens to show, computed on the server so
/// the browser never invents the rule. Answers "does this browser have a run it could continue,
/// and did it finish one before?" - both, either, or neither is a normal answer, never a 404.
///
/// LearnerKey never appears here, in either direction of the pair: it is the browser's own key,
/// and echoing it back would put it in a payload that has no use for it.
/// </summary>
public sealed class LearningResumeStateViewModel
{
    public required PublicTrainingLinkViewModel Link { get; init; }

    /// <summary>A run still IN_PROGRESS. Non-null means the join screen MUST ask "คุณคือ ... ใช่ไหม"
    /// before letting anyone continue - the key lives in the browser, not in a person, and a shared
    /// school computer hands the same key to whoever sits down next.</summary>
    public LearningSessionViewModel? Resumable { get; init; }

    /// <summary>The last finished run. Only consulted when Resumable is null - there is nothing to
    /// confirm about a round that already ended.</summary>
    public LearningSessionViewModel? LastEnded { get; init; }

    /// <summary>Starting something new is blocked past this point, but finishing a run that began
    /// while the link was valid is not.</summary>
    public required bool LinkExpired { get; init; }
}

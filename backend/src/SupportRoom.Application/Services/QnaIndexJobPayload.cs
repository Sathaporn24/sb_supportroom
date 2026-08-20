namespace SupportRoom.Application.Services;

/// <summary>
/// QQ-6 - BackgroundJob.PayloadJson shape for JobType = qna_index. TargetId is the KnowledgeQnA.Id.
///
/// NeedsReEmbed exists purely so editing only the Answer skips a paid embedding call: KS-5 embeds
/// the Question alone, so if the Question did not change the existing vector is still correct and
/// only the "ถาม: ...\nตอบ: ..." text stored alongside it needs to catch up
/// (IKnowledgeIndexProvider.UpdateMetadataAsync does that without re-embedding). True on first
/// index (there is nothing to reuse yet) and whenever Question changes.
/// </summary>
public sealed class QnaIndexJobPayload
{
    public required bool NeedsReEmbed { get; init; }
}

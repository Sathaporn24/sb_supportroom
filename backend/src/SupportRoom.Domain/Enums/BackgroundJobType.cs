namespace SupportRoom.Domain.Enums;

/// <summary>What a BackgroundJob does, keyed off BackgroundJob.TargetId - see design.md DM-11.
/// document_index/vector_delete are consumed starting Phase 3; lesson_index/qna_index are declared
/// here now so Phase 5/6 don't need another migration, but nothing enqueues them yet.</summary>
public static class BackgroundJobType
{
    public const string DocumentIndex = "document_index";
    public const string LessonIndex = "lesson_index";
    public const string QnaIndex = "qna_index";
    public const string VectorDelete = "vector_delete";

    /// <summary>R9/Module L - the durable purge that runs 60 days after a lesson is archived
    /// (LT-3), deleting its vectors/storage bytes/DB rows for good (LT-15..LT-19).</summary>
    public const string LessonPurge = "lesson_purge";
}

namespace SupportRoom.Domain.Enums;

/// <summary>KS-6 - every chunk indexed from now on carries metadata.sourceType so the answer step
/// can tell a document/slide chunk apart from a Q&A chunk once they start sharing namespaces.
/// Vectors indexed before this field existed have no sourceType key at all - readers must treat
/// that as Document (see the resolver in RagVoiceQuestionProvider), never throw or drop the
/// chunk.</summary>
public static class KnowledgeSourceType
{
    public const string Document = "document";
    public const string Slide = "slide";
    public const string Qna = "qna";
}

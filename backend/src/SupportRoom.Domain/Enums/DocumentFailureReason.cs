namespace SupportRoom.Domain.Enums;

public static class DocumentFailureReason
{
    public const string UnsupportedType = "unsupported_type";
    public const string ExtractFailed = "extract_failed";
    public const string NoText = "no_text";
    public const string EmbeddingFailed = "embedding_failed";
    public const string IndexFailed = "index_failed";
}

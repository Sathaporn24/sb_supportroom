namespace SupportRoom.Application.Common;

/// <summary>
/// KL-12 - shared by DocumentResourceService and KnowledgeQnAService's content search: a trimmed-
/// empty query and a query under 2 characters both mean "no search, return everything per the
/// other filters" - neither is an error.
/// </summary>
public static class KnowledgeLibrarySearch
{
    public const int MinQueryLength = 2;

    public static string? Normalize(string? q)
    {
        if (q is null)
        {
            return null;
        }

        var trimmed = q.Trim();
        return trimmed.Length < MinQueryLength ? null : trimmed;
    }
}

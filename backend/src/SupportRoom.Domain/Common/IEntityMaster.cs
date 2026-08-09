namespace SupportRoom.Domain.Common;

/// <summary>
/// Local equivalent of BossupStandard's IEntityMaster&lt;TKey&gt; (see .claude/skills/
/// dotnet-layered-backend/SKILL.md) - generic over TKey instead of a hardcoded Guid because
/// every id in this project comes from SupportRoom.Infrastructure/IdGenerator.cs as a
/// prefixed string, not a Guid.
/// </summary>
public interface IEntityMaster<TKey>
{
    TKey Id { get; }
    string? CreateBy { get; }
    DateTime CreateDate { get; }
    string? UpdateBy { get; }
    DateTime? UpdateDate { get; }
    string? DeleteBy { get; }
    bool IsDelete { get; }
    DateTime? DeletedAt { get; }
}

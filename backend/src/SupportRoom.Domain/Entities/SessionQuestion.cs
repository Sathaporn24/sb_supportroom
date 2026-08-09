using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

public sealed class SessionQuestion : IEntityMaster<string>
{
    public required string Id { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; init; }
    public DateTime? UpdateDate { get; init; }
    public string? DeleteBy { get; init; }
    public bool IsDelete { get; init; }
    public DateTime? DeletedAt { get; init; }

    public required string SessionId { get; init; }
    public string? SlideObjectId { get; init; }
    public string? Transcript { get; init; }
    public string? Answer { get; init; }
    public required string AnswerStatus { get; init; }
}

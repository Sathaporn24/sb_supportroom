using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// Backup text channel for a session - separate from SessionQuestion (the Push-to-Talk log).
/// Persisted so a CS agent joining mid-session, or reconnecting, still sees history.
/// </summary>
public sealed class ChatMessage : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; init; }
    public DateTime? UpdateDate { get; init; }
    public string? DeleteBy { get; init; }
    public bool IsDelete { get; init; }
    public DateTime? DeletedAt { get; init; }

    public required string SessionId { get; init; }
    public required string SenderRole { get; init; }
    public string? SenderName { get; init; }
    public required string Text { get; init; }
}

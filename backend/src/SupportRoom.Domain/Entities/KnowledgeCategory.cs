using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

public sealed class KnowledgeCategory : IEntityMaster<string>, ICompanyScoped
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
    public string? ParentId { get; init; }
    public required int Level { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int SortOrder { get; set; }
    public required bool IsSystemDefault { get; init; }
}

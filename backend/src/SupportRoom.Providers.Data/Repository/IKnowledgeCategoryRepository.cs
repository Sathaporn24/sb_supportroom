using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IKnowledgeCategoryRepository : IRepositoryBase<KnowledgeCategory, string>
{
    IQueryable<KnowledgeCategory> GetByCompanyOrdered();
    IQueryable<KnowledgeCategory> GetChildren(string parentId);
    KnowledgeCategory? GetSystemDefault();
}

public sealed class KnowledgeCategoryRepository(ApplicationDbContext dbContext)
    : RepositoryBase<KnowledgeCategory, string>(dbContext), IKnowledgeCategoryRepository
{
    public IQueryable<KnowledgeCategory> GetByCompanyOrdered()
        => GetAll().OrderBy(x => x.Level).ThenBy(x => x.SortOrder).ThenBy(x => x.Name);

    public IQueryable<KnowledgeCategory> GetChildren(string parentId)
        => FindBy(x => x.ParentId == parentId);

    /// <summary>The backfill migration flags TWO rows per company as IsSystemDefault - the
    /// Level-1 parent "ยังไม่จัดหมวด" and its Level-2 leaf child, linked together as a chain.
    /// LessonConfig.CategoryId always points at the leaf, so a bare IsSystemDefault filter matches
    /// both rows and SingleOrDefault throws. Level == 2 picks the one callers actually want.</summary>
    public KnowledgeCategory? GetSystemDefault()
        => FindBy(x => x.IsSystemDefault && x.Level == 2).SingleOrDefault();
}

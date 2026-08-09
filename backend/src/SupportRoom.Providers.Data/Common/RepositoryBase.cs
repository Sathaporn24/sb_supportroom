using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Common;

/// <summary>
/// Local equivalent of BossupStandard's RepositoryBase&lt;Entity, TDbContext&gt; (see
/// .claude/skills/dotnet-layered-backend/SKILL.md) - only query logic belongs here, business
/// logic/validation stays in the Service layer.
/// </summary>
public abstract class RepositoryBase<TEntity, TKey>(ApplicationDbContext dbContext) : IRepositoryBase<TEntity, TKey>
    where TEntity : class
{
    protected readonly ApplicationDbContext Context = dbContext;
    private readonly DbSet<TEntity> _set = dbContext.Set<TEntity>();

    public IQueryable<TEntity> GetAll() => _set.AsQueryable();

    public IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate) => _set.Where(predicate);

    public TEntity? Get(TKey id) => _set.Find(id);

    public async Task<TEntity?> GetAsync(TKey id) => await _set.FindAsync(id);

    public void Add(TEntity entity) => _set.Add(entity);

    public void Update(TEntity entity) => _set.Update(entity);

    public void Delete(TEntity entity) => _set.Remove(entity);
}

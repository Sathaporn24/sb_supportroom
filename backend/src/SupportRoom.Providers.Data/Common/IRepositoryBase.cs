using System.Linq.Expressions;

namespace SupportRoom.Providers.Data.Common;

public interface IRepositoryBase<TEntity, in TKey>
    where TEntity : class
{
    IQueryable<TEntity> GetAll();
    IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate);
    TEntity? Get(TKey id);
    Task<TEntity?> GetAsync(TKey id);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}

namespace SupportRoom.Providers.Data.Data.UnitOfWork;

public interface IUnitOfWork
{
    TRepository GetRepository<TRepository>();
    void Commit();
}

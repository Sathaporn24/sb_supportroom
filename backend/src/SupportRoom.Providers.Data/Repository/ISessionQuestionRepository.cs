using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ISessionQuestionRepository : IRepositoryBase<SessionQuestion, string>
{
    IQueryable<SessionQuestion> GetBySessionId(string sessionId);
}

public sealed class SessionQuestionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<SessionQuestion, string>(dbContext), ISessionQuestionRepository
{
    public IQueryable<SessionQuestion> GetBySessionId(string sessionId)
        => FindBy(x => x.SessionId == sessionId);
}

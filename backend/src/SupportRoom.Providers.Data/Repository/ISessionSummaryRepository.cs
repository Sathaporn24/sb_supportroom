using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface ISessionSummaryRepository : IRepositoryBase<SessionSummary, string>
{
    SessionSummary? GetBySessionId(string sessionId);
}

public sealed class SessionSummaryRepository(ApplicationDbContext dbContext)
    : RepositoryBase<SessionSummary, string>(dbContext), ISessionSummaryRepository
{
    public SessionSummary? GetBySessionId(string sessionId)
        => FindBy(x => x.SessionId == sessionId).SingleOrDefault();
}

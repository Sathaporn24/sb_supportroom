using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Common;
using SupportRoom.Providers.Data.Data;

namespace SupportRoom.Providers.Data.Repository;

public interface IChatMessageRepository : IRepositoryBase<ChatMessage, string>
{
    IQueryable<ChatMessage> GetBySessionId(string sessionId);
}

public sealed class ChatMessageRepository(ApplicationDbContext dbContext)
    : RepositoryBase<ChatMessage, string>(dbContext), IChatMessageRepository
{
    public IQueryable<ChatMessage> GetBySessionId(string sessionId)
        => FindBy(x => x.SessionId == sessionId);
}

using Mapster;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface IChatMessageService
{
    Task<ChatMessageViewModel> SendAsync(SendChatMessageDto input);
    IReadOnlyList<ChatMessageViewModel> GetBySessionId(string sessionId);
}

public sealed class ChatMessageService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IChatMessageService> logger,
    IRealtimeNotifier realtimeNotifier)
    : ServiceBase<IChatMessageService>(unitOfWork, serviceProvider, logger), IChatMessageService
{
    private readonly IChatMessageRepository _repository = unitOfWork.GetRepository<IChatMessageRepository>();
    private readonly ITrainingSessionRepository _sessionRepository = unitOfWork.GetRepository<ITrainingSessionRepository>();

    public async Task<ChatMessageViewModel> SendAsync(SendChatMessageDto input)
    {
        var session = _sessionRepository.Get(input.SessionId) ?? throw GeneralException.NotFound("เซสชัน");

        var entity = new ChatMessage
        {
            Id = IdGenerator.GenerateId("chat"),
            SessionId = input.SessionId,
            SenderRole = input.SenderRole,
            SenderName = input.SenderName,
            Text = input.Text,
            CreateDate = DateTime.UtcNow,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();

        // Never log Text - chat is a live conversation channel, not something ops logs should hold.
        Logger.LogInformation("Chat message sent: session={SessionId} sender={SenderRole}", input.SessionId, input.SenderRole);

        var viewModel = entity.Adapt<ChatMessageViewModel>();
        await realtimeNotifier.NotifyChatMessageAsync(session.Token, viewModel);
        return viewModel;
    }

    public IReadOnlyList<ChatMessageViewModel> GetBySessionId(string sessionId)
        => _repository.GetBySessionId(sessionId).OrderBy(x => x.CreateDate).ToList().Adapt<List<ChatMessageViewModel>>();
}

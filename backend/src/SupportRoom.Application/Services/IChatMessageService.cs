using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>The learner's own chat history. Keyed on (token, learnerKey) rather than the token
    /// alone so two people on the same link cannot read each other's conversation.</summary>
    IReadOnlyList<ChatMessageViewModel> GetForLearner(string token, string learnerKey);

    /// <summary>CS-facing: one learning session's chat history.</summary>
    IReadOnlyList<ChatMessageViewModel> GetByLearningSessionId(string learningSessionId);
}

public sealed class ChatMessageService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IChatMessageService> logger,
    IRealtimeNotifier realtimeNotifier)
    : ServiceBase<IChatMessageService>(unitOfWork, serviceProvider, logger), IChatMessageService
{
    private readonly IChatMessageRepository _repository = unitOfWork.GetRepository<IChatMessageRepository>();
    private readonly ILearningSessionRepository _learningSessionRepository = unitOfWork.GetRepository<ILearningSessionRepository>();

    public async Task<ChatMessageViewModel> SendAsync(SendChatMessageDto input)
    {
        var session = _learningSessionRepository.Get(input.SessionId) ?? throw GeneralException.NotFound("การเรียน");

        var entity = new ChatMessage
        {
            Id = IdGenerator.GenerateId("chat"),
            CompanyId = CurrentCompanyId,
            SessionId = input.SessionId,
            SenderRole = input.SenderRole,
            SenderName = input.SenderName,
            Text = input.Text,
            // Deliberately unconditional: null when the learner typed it (they have no
            // account), the agent's user id when CS did. No branching needed - the absence of
            // a signed-in user IS the answer.
            CreateBy = CurrentUserId,
            CreateDate = DateTime.UtcNow,
        };

        _repository.Add(entity);
        UnitOfWork.Commit();

        // Never log Text - chat is a live conversation channel, not something ops logs should hold.
        Logger.LogInformation("Chat message sent: session={SessionId} sender={SenderRole}", input.SessionId, input.SenderRole);

        var viewModel = entity.Adapt<ChatMessageViewModel>();
        // Broadcast to the learning session's own group, not the link's - see IRealtimeNotifier.
        await realtimeNotifier.NotifyChatMessageAsync(session.Id, viewModel);
        return viewModel;
    }

    public IReadOnlyList<ChatMessageViewModel> GetForLearner(string token, string learnerKey)
    {
        var session = ServiceProvider.GetRequiredService<ILearningSessionService>().GetEntityByLearnerKey(token, learnerKey);
        return GetByLearningSessionId(session.Id);
    }

    public IReadOnlyList<ChatMessageViewModel> GetByLearningSessionId(string learningSessionId)
        => _repository.GetBySessionId(learningSessionId)
            .OrderBy(x => x.CreateDate)
            .ToList()
            .Adapt<List<ChatMessageViewModel>>();
}

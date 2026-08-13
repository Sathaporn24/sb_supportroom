using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Api.Hubs;

/// <summary>
/// One SignalR group per LEARNING SESSION id.
///
/// It used to be one group per link token, which was correct only while a link meant a single
/// learner. Now that one link is opened by a whole department, a token-keyed group would put
/// every learner in the same room and fan each person's questions and chat out to all of them -
/// the exact leak CORE_FEATURE_SPEC §2.4 exists to prevent.
///
/// The group key is derived server-side from (token, learnerKey), never accepted from the client,
/// so joining someone else's group means guessing their browser key.
///
/// Broadcasts happen from Application services via IRealtimeNotifier, not from this Hub directly -
/// "ReceiveNewQuestion" (live Push-to-Talk Q&amp;A) and "ReceiveChatMessage" (typed chat, sent below).
/// </summary>
public sealed class SessionHub(IServiceProvider serviceProvider) : Hub
{
    public async Task JoinSession(string token, string learnerKey)
    {
        var learningSession = ResolveLearningSessionId(token, learnerKey);
        await Groups.AddToGroupAsync(Context.ConnectionId, learningSession);
    }

    public async Task SendChatMessage(string token, string learnerKey, string senderRole, string? senderName, string text)
    {
        var learningSessionId = ResolveLearningSessionId(token, learnerKey);
        var chatMessageService = serviceProvider.GetRequiredService<IChatMessageService>();

        try
        {
            await chatMessageService.SendAsync(new SendChatMessageDto
            {
                SessionId = learningSessionId,
                SenderRole = senderRole,
                SenderName = senderName,
                Text = text,
            });
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    /// <summary>
    /// CS-side entry point. A support agent has no learnerKey - that key belongs to the learner's
    /// browser - so they address a learning session by id instead.
    ///
    /// ⚠️ Unauthenticated, exactly like the rest of /admin today (TD-002). The id is an
    /// unguessable GUID, which is what stands in for access control until auth lands; when it
    /// does, these two methods are the ones that need an agent claim check. Do not hand the id
    /// to a learner-facing surface in the meantime.
    /// </summary>
    public async Task JoinSessionAsAgent(string learningSessionId)
    {
        EnsureLearningSessionExists(learningSessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, learningSessionId);
    }

    public async Task SendChatMessageAsAgent(string learningSessionId, string? senderName, string text)
    {
        EnsureLearningSessionExists(learningSessionId);
        var chatMessageService = serviceProvider.GetRequiredService<IChatMessageService>();

        try
        {
            await chatMessageService.SendAsync(new SendChatMessageDto
            {
                SessionId = learningSessionId,
                SenderRole = ChatSenderRole.Agent,
                SenderName = senderName,
                Text = text,
            });
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    private void EnsureLearningSessionExists(string learningSessionId)
    {
        var learningSessionService = serviceProvider.GetRequiredService<ILearningSessionService>();
        try
        {
            learningSessionService.GetById(learningSessionId);
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    private string ResolveLearningSessionId(string token, string learnerKey)
    {
        var learningSessionService = serviceProvider.GetRequiredService<ILearningSessionService>();
        try
        {
            return learningSessionService.GetEntityByLearnerKey(token, learnerKey).Id;
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}

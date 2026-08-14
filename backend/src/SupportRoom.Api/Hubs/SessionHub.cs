using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Common;
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
[AllowAnonymous]
public sealed class SessionHub(IServiceProvider serviceProvider) : Hub
{
    public async Task JoinSession(string token, string learnerKey)
    {
        var learningSession = ResolveLearningSessionId(token, learnerKey);
        await Groups.AddToGroupAsync(Context.ConnectionId, learningSession);
    }

    public async Task SendChatMessage(string token, string learnerKey, string text)
    {
        var learningSession = ResolveLearningSession(token, learnerKey);
        var chatMessageService = serviceProvider.GetRequiredService<IChatMessageService>();

        try
        {
            await chatMessageService.SendAsync(new SendChatMessageDto
            {
                SessionId = learningSession.Id,
                // A learner is always a recipient. Trusting this argument lets anyone holding a
                // link label their own message as an agent/system message in everybody's UI.
                SenderRole = ChatSenderRole.Recipient,
                // The session row is the canonical learner identity. Do not trust a second name
                // sent over the socket, even though changing it would only spoof the caller.
                SenderName = learningSession.RecipientName,
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
    /// The hub itself stays anonymous because learners have no account. Agent entry points must
    /// therefore opt into the same authorization guard as REST; a GUID is not access control.
    /// </summary>
    public async Task JoinSessionAsAgent(string learningSessionId)
    {
        EnsureAgentAuthenticated();
        EnsureLearningSessionExists(learningSessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, learningSessionId);
    }

    public async Task SendChatMessageAsAgent(string learningSessionId, string text)
    {
        EnsureAgentAuthenticated();
        EnsureLearningSessionExists(learningSessionId);
        var chatMessageService = serviceProvider.GetRequiredService<IChatMessageService>();

        try
        {
            await chatMessageService.SendAsync(new SendChatMessageDto
            {
                SessionId = learningSessionId,
                SenderRole = ChatSenderRole.Agent,
                SenderName = Context.User?.FindFirst(AuthClaims.DisplayName)?.Value ?? "ทีมซัพพอร์ต",
                Text = text,
            });
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    private void EnsureAgentAuthenticated()
    {
        var guard = serviceProvider.GetRequiredService<IAuthorizationGuard>();
        try
        {
            guard.EnsureAuthenticated();
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
        => ResolveLearningSession(token, learnerKey).Id;

    private SupportRoom.Domain.Entities.LearningSession ResolveLearningSession(string token, string learnerKey)
    {
        var learningSessionService = serviceProvider.GetRequiredService<ILearningSessionService>();
        try
        {
            return learningSessionService.GetEntityByLearnerKey(token, learnerKey);
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}

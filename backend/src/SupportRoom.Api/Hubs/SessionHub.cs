using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Hubs;

/// <summary>
/// One SignalR group per session Token - the existing public join-link identifier, safe to
/// expose as a group key since the rest of the app (admin, room) is already unauthenticated.
/// Broadcasts happen from Application services via IRealtimeNotifier, not from this Hub
/// directly - "ReceiveNewQuestion" (live Push-to-Talk Q&amp;A) and "ReceiveChatMessage" (typed
/// chat, sent below).
/// </summary>
public sealed class SessionHub(IServiceProvider serviceProvider) : Hub
{
    public async Task JoinSession(string token)
    {
        var sessionService = serviceProvider.GetRequiredService<ITrainingSessionService>();
        try
        {
            sessionService.GetByToken(token);
        }
        catch (HttpStatusCodeException ex)
        {
            throw new HubException(ex.Message);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, token);
    }

    public async Task SendChatMessage(string token, string senderRole, string? senderName, string text)
    {
        var sessionService = serviceProvider.GetRequiredService<ITrainingSessionService>();
        var chatMessageService = serviceProvider.GetRequiredService<IChatMessageService>();

        try
        {
            var session = sessionService.GetByToken(token);
            await chatMessageService.SendAsync(new SendChatMessageDto
            {
                SessionId = session.Id,
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
}

using Microsoft.AspNetCore.SignalR;
using SupportRoom.Api.Hubs;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.ViewModel;

namespace SupportRoom.Api.Realtime;

/// <summary>Real IRealtimeNotifier implementation - needs IHubContext, a framework type only
/// available where the Hub itself lives (Api), which is why the port exists in Application.</summary>
public sealed class SignalRRealtimeNotifier(IHubContext<SessionHub> hubContext) : IRealtimeNotifier
{
    public Task NotifyNewQuestionAsync(string sessionToken, SessionQuestionViewModel question)
        => hubContext.Clients.Group(sessionToken).SendAsync("ReceiveNewQuestion", question);

    public Task NotifyChatMessageAsync(string sessionToken, ChatMessageViewModel message)
        => hubContext.Clients.Group(sessionToken).SendAsync("ReceiveChatMessage", message);
}

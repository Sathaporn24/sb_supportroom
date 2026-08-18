using Microsoft.AspNetCore.SignalR;
using SupportRoom.Api.Hubs;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.ViewModel;

namespace SupportRoom.Api.Realtime;

/// <summary>Real IRealtimeNotifier implementation - needs IHubContext, a framework type only
/// available where the Hub itself lives (Api), which is why the port exists in Application.</summary>
public sealed class SignalRRealtimeNotifier(IHubContext<SessionHub> hubContext) : IRealtimeNotifier
{
    public Task NotifyNewQuestionAsync(string learningSessionId, SessionQuestionViewModel question)
        => hubContext.Clients.Group(learningSessionId).SendAsync("ReceiveNewQuestion", question);

    public Task NotifyChatMessageAsync(string learningSessionId, ChatMessageViewModel message)
        => hubContext.Clients.Group(learningSessionId).SendAsync("ReceiveChatMessage", message);
}

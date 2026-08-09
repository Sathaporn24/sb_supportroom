using SupportRoom.Application.ViewModel;

namespace SupportRoom.Application.Realtime;

/// <summary>
/// Port so Application-layer services can trigger a live broadcast without referencing
/// SupportRoom.Api (where the SignalR Hub actually lives - Application cannot depend on Api,
/// that would invert the layering). Same shape as the Provider pattern: interface here, the
/// real SignalR-backed implementation is wired via DI in Api/Configurations/ServiceConfiguration.cs.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyNewQuestionAsync(string sessionToken, SessionQuestionViewModel question);
    Task NotifyChatMessageAsync(string sessionToken, ChatMessageViewModel message);
}

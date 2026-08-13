using SupportRoom.Application.ViewModel;

namespace SupportRoom.Application.Realtime;

/// <summary>
/// Port so Application-layer services can trigger a live broadcast without referencing
/// SupportRoom.Api (where the SignalR Hub actually lives - Application cannot depend on Api,
/// that would invert the layering). Same shape as the Provider pattern: interface here, the
/// real SignalR-backed implementation is wired via DI in Api/Configurations/ServiceConfiguration.cs.
///
/// ⚠️ The group key is a LEARNING SESSION id, not a link token. It used to be the token, which
/// was correct only while one link meant one learner: once a link is opened by a whole
/// department, a token-keyed group fans every person's questions and chat out to everyone else
/// who happens to hold the same link.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyNewQuestionAsync(string learningSessionId, SessionQuestionViewModel question);
    Task NotifyChatMessageAsync(string learningSessionId, ChatMessageViewModel message);
}

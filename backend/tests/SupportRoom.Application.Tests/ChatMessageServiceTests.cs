using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

public class ChatMessageServiceTests
{
    private readonly FakeChatMessageRepository _messages = new();
    private readonly FakeTrainingSessionRepository _sessions = new();
    private readonly FakeRealtimeNotifier _notifier = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ChatMessageService _service;

    public ChatMessageServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<IChatMessageRepository>(_messages)
            .Register<ITrainingSessionRepository>(_sessions)
            .Register<ILessonConfigRepository>(new FakeLessonConfigRepository());
        // Real TrainingSessionService, not a fake: GetByToken is the step that resolves the
        // company for the whole request, so stubbing it out would hide the behaviour being relied on.
        var serviceProvider = new FakeServiceProvider();
        serviceProvider.Register<ITrainingSessionService>(
            new TrainingSessionService(_unitOfWork, serviceProvider, NullLogger<ITrainingSessionService>.Instance));
        _service = new ChatMessageService(_unitOfWork, serviceProvider, NullLogger<IChatMessageService>.Instance, _notifier);
    }

    private void SeedSession(string id = "session-1", string token = "tok-1")
        => _sessions.Items.Add(new TrainingSession
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            Token = token,
            LessonId = "lesson-1",
            LessonSlug = "lesson-a",
            Status = SessionStatus.InProgress,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });

    [Fact]
    public async Task SendAsync_ThrowsNotFound_WhenSessionMissing()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.SendAsync(new SendChatMessageDto { SessionId = "ghost", SenderRole = "cs", Text = "hi" }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task SendAsync_PersistsCommitsAndBroadcastsToTheSessionToken()
    {
        SeedSession("session-1", "tok-1");

        var vm = await _service.SendAsync(new SendChatMessageDto
        {
            SessionId = "session-1",
            SenderRole = "cs",
            SenderName = "ทีม CS",
            Text = "สวัสดีค่ะ",
        });

        Assert.Equal("สวัสดีค่ะ", vm.Text);
        Assert.Single(_messages.Items);
        Assert.Equal(1, _unitOfWork.CommitCount);
        Assert.Equal(1, _notifier.ChatMessageCount);
        Assert.Equal("tok-1", _notifier.LastChatToken); // broadcast keyed by the public token, not the id
    }

    [Fact]
    public void GetByToken_ReturnsMessagesOldestFirst()
    {
        SeedSession("session-1", "tok-1");
        _messages.Items.Add(new ChatMessage { Id = "m-late", CompanyId = TestFixtures.CompanyId, SessionId = "session-1", SenderRole = "cs", Text = "b", CreateDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
        _messages.Items.Add(new ChatMessage { Id = "m-early", CompanyId = TestFixtures.CompanyId, SessionId = "session-1", SenderRole = "teacher", Text = "a", CreateDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        var list = _service.GetByToken("tok-1");

        Assert.Equal("m-early", list[0].Id);
        Assert.Equal("m-late", list[1].Id);
    }
}

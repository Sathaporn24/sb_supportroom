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
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeRealtimeNotifier _notifier = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ChatMessageService _service;

    public ChatMessageServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<IChatMessageRepository>(_messages)
            .Register<ITrainingLinkRepository>(_links)
            .Register<ILearningSessionRepository>(_learningSessions)
            .Register<ISessionQuestionRepository>(new FakeSessionQuestionRepository())
            .Register<ILessonConfigRepository>(new FakeLessonConfigRepository());
        // Real services, not fakes: GetEntityByToken is the step that resolves the company for the
        // whole request, so stubbing it out would hide the behaviour being relied on.
        var serviceProvider = new FakeServiceProvider();
        var linkService = new TrainingLinkService(_unitOfWork, serviceProvider, NullLogger<ITrainingLinkService>.Instance);
        serviceProvider.Register<ITrainingLinkService>(linkService);
        serviceProvider.Register<ILearningSessionService>(
            new LearningSessionService(_unitOfWork, serviceProvider, NullLogger<ILearningSessionService>.Instance));
        _service = new ChatMessageService(_unitOfWork, serviceProvider, NullLogger<IChatMessageService>.Instance, _notifier);
    }

    private void Seed(string learningSessionId = "learning-1", string token = "tok-1", string learnerKey = "key-1")
    {
        _links.Items.Add(new TrainingLink
        {
            Id = "link-1",
            CompanyId = TestFixtures.CompanyId,
            Token = token,
            LessonId = "lesson-1",
            LessonSlug = "lesson-a",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        _learningSessions.Items.Add(new LearningSession
        {
            Id = learningSessionId,
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = "link-1",
            LearnerKey = learnerKey,
            RecipientName = "ครูเอ",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });
    }

    [Fact]
    public async Task SendAsync_ThrowsNotFound_WhenLearningSessionMissing()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.SendAsync(new SendChatMessageDto { SessionId = "ghost", SenderRole = ChatSenderRole.Agent, Text = "hi" }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ValidatesHubInputEvenWithoutControllerModelBinding()
    {
        Seed();

        var blank = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.SendAsync(new SendChatMessageDto
            {
                SessionId = "learning-1",
                SenderRole = ChatSenderRole.Recipient,
                Text = " ",
            }));
        var invalidRole = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.SendAsync(new SendChatMessageDto
            {
                SessionId = "learning-1",
                SenderRole = "spoofed",
                Text = "hello",
            }));

        Assert.Equal(400, (int)blank.StatusCode);
        Assert.Equal(400, (int)invalidRole.StatusCode);
        Assert.Empty(_messages.Items);
    }

    [Fact]
    public async Task SendAsync_BroadcastsToTheLearningSession_NotTheLinkToken()
    {
        Seed("learning-1", "tok-1");

        var vm = await _service.SendAsync(new SendChatMessageDto
        {
            SessionId = "learning-1",
            SenderRole = ChatSenderRole.Agent,
            SenderName = "ทีม CS",
            Text = "สวัสดีค่ะ",
        });

        Assert.Equal("สวัสดีค่ะ", vm.Text);
        Assert.Single(_messages.Items);
        Assert.Equal(1, _unitOfWork.CommitCount);
        Assert.Equal(1, _notifier.ChatMessageCount);
        // The regression this guards: keyed by the link token, every learner on that link would
        // receive this message.
        Assert.Equal("learning-1", _notifier.LastChatTarget);
        Assert.NotEqual("tok-1", _notifier.LastChatTarget);
    }

    [Fact]
    public async Task SendAsync_RefusesMessagesAfterTheLearningSessionEnded()
    {
        Seed("learning-1", "tok-1");
        _learningSessions.Items.Single().Status = SessionStatus.Ended;

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.SendAsync(new SendChatMessageDto
            {
                SessionId = "learning-1",
                SenderRole = ChatSenderRole.Recipient,
                Text = "ส่งหลังจบ",
            }));

        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_messages.Items);
    }

    [Fact]
    public void GetForLearner_ReturnsMessagesOldestFirst()
    {
        Seed("learning-1", "tok-1", "key-1");
        _messages.Items.Add(new ChatMessage { Id = "m-late", CompanyId = TestFixtures.CompanyId, SessionId = "learning-1", SenderRole = ChatSenderRole.Agent, Text = "b", CreateDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
        _messages.Items.Add(new ChatMessage { Id = "m-early", CompanyId = TestFixtures.CompanyId, SessionId = "learning-1", SenderRole = ChatSenderRole.Recipient, Text = "a", CreateDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        var list = _service.GetForLearner("tok-1", "key-1");

        Assert.Equal("m-early", list[0].Id);
        Assert.Equal("m-late", list[1].Id);
    }

    [Fact]
    public void GetForLearner_CannotReadAnotherLearnersChatOnTheSameLink()
    {
        Seed("learning-a", "tok-1", "key-a");
        _learningSessions.Items.Add(new LearningSession
        {
            Id = "learning-b",
            CompanyId = TestFixtures.CompanyId,
            TrainingLinkId = "link-1",
            LearnerKey = "key-b",
            RecipientName = "ครูบี",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
        });
        _messages.Items.Add(new ChatMessage { Id = "m-a", CompanyId = TestFixtures.CompanyId, SessionId = "learning-a", SenderRole = ChatSenderRole.Recipient, Text = "ของเอ", CreateDate = DateTime.UtcNow });
        _messages.Items.Add(new ChatMessage { Id = "m-b", CompanyId = TestFixtures.CompanyId, SessionId = "learning-b", SenderRole = ChatSenderRole.Recipient, Text = "ของบี", CreateDate = DateTime.UtcNow });

        Assert.Equal("m-a", Assert.Single(_service.GetForLearner("tok-1", "key-a")).Id);
        Assert.Equal("m-b", Assert.Single(_service.GetForLearner("tok-1", "key-b")).Id);
    }
}

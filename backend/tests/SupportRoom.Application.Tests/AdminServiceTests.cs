using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

/// <summary>
/// ResetDemoData reads ALLOW_DATA_RESET from the process environment. Each test sets it explicitly
/// and restores it in a finally so a leaked value can't affect another test (or the dev's shell).
/// </summary>
public class AdminServiceTests
{
    private readonly FakeTrainingLinkRepository _links = new();
    private readonly FakeLearningSessionRepository _learningSessions = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeChatMessageRepository _chatMessages = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _unitOfWork
            .Register<ITrainingLinkRepository>(_links)
            .Register<ILearningSessionRepository>(_learningSessions)
            .Register<ISessionQuestionRepository>(_questions)
            .Register<IChatMessageRepository>(_chatMessages);
        _service = new AdminService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IAdminService>.Instance,
            // Reset and reindex are owner-only (TD-014); these tests exercise the rest of the
            // behaviour, so they run as an owner. The refusal path has its own tests in
            // AuthorizationTests.
            TestFixtures.OwnerGuard(),
            new FakeDocumentStorageProvider(),
            new FakeKnowledgeIndexingService(),
            new FakeKnowledgeIndexProvider(),
            new FakeSlidesProvider());
    }

    private static void WithResetFlag(string? value, Action body)
    {
        var previous = Environment.GetEnvironmentVariable("ALLOW_DATA_RESET");
        Environment.SetEnvironmentVariable("ALLOW_DATA_RESET", value);
        try { body(); }
        finally { Environment.SetEnvironmentVariable("ALLOW_DATA_RESET", previous); }
    }

    [Fact]
    public void ResetDemoData_IsBlocked_WhenFlagNotSet()
        => WithResetFlag(null, () =>
        {
            var ex = Assert.Throws<HttpStatusCodeException>(() => _service.ResetDemoData());
            Assert.Equal(ApiErrorCode.ConfigError, ex.Code);
        });

    [Fact]
    public void ResetDemoData_DeletesLinksSessionsQuestionsAndChat_WhenEnabled()
        => WithResetFlag("true", () =>
        {
            _links.Items.Add(new TrainingLink { Id = "link-1", CompanyId = TestFixtures.CompanyId, Token = "t1", LessonId = "l", LessonSlug = "a", ExpiresAt = DateTime.UtcNow });
            _learningSessions.Items.Add(new LearningSession
            {
                Id = "learning-1",
                CompanyId = TestFixtures.CompanyId,
                TrainingLinkId = "link-1",
                LearnerKey = "key-1",
                RecipientName = "ครูเอ",
                Status = SessionStatus.Ended,
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
            });
            _questions.Items.Add(new SessionQuestion { Id = "q1", CompanyId = TestFixtures.CompanyId, SessionId = "learning-1", AnswerStatus = AnswerStatus.Answered });
            _chatMessages.Items.Add(new ChatMessage { Id = "c1", CompanyId = TestFixtures.CompanyId, SessionId = "learning-1", SenderRole = ChatSenderRole.Recipient, Text = "hi" });

            _service.ResetDemoData();

            Assert.Empty(_links.Items);
            Assert.Empty(_learningSessions.Items);
            Assert.Empty(_questions.Items);
            Assert.Empty(_chatMessages.Items);
            Assert.Equal(1, _unitOfWork.CommitCount);
        });
}

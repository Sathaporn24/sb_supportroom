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
    private readonly FakeTrainingSessionRepository _sessions = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeSessionSummaryRepository _summaries = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _unitOfWork
            .Register<ITrainingSessionRepository>(_sessions)
            .Register<ISessionQuestionRepository>(_questions)
            .Register<ISessionSummaryRepository>(_summaries);
        _service = new AdminService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IAdminService>.Instance,
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
    public void ResetDemoData_DeletesSessionsQuestionsSummaries_WhenEnabled()
        => WithResetFlag("true", () =>
        {
            _sessions.Items.Add(new TrainingSession { Id = "s1", CompanyId = TestFixtures.CompanyId, Token = "t1", LessonId = "l", LessonSlug = "a", Status = SessionStatus.Ended, ExpiresAt = DateTime.UtcNow });
            _questions.Items.Add(new SessionQuestion { Id = "q1", CompanyId = TestFixtures.CompanyId, SessionId = "s1", AnswerStatus = AnswerStatus.Answered });
            _summaries.Items.Add(new SessionSummary { Id = "sum1", CompanyId = TestFixtures.CompanyId, SessionId = "s1", CompletedAllSlides = true, UnansweredPoints = [] });

            _service.ResetDemoData();

            Assert.Empty(_sessions.Items);
            Assert.Empty(_questions.Items);
            Assert.Empty(_summaries.Items);
            Assert.Equal(1, _unitOfWork.CommitCount);
        });
}

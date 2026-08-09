using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

public class SessionSummaryServiceTests
{
    private readonly FakeSessionSummaryRepository _summaries = new();
    private readonly FakeSessionQuestionRepository _questions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly SessionSummaryService _service;

    public SessionSummaryServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<ISessionSummaryRepository>(_summaries)
            .Register<ISessionQuestionRepository>(_questions);
        _service = new SessionSummaryService(_unitOfWork, new FakeServiceProvider(), NullLogger<ISessionSummaryService>.Instance);
    }

    private void AddQuestion(string id, string status, string? transcript)
        => _questions.Items.Add(new SessionQuestion
        {
            Id = id,
            SessionId = "session-1",
            AnswerStatus = status,
            Transcript = transcript,
            CreateDate = DateTime.UtcNow,
        });

    [Fact]
    public void Save_CollectsOnlyNotFoundQuestionsAsUnansweredPoints()
    {
        AddQuestion("q1", AnswerStatus.Answered, "ตอบได้");
        AddQuestion("q2", AnswerStatus.NotFound, "ถามเรื่องที่ไม่มีในบทเรียน");
        AddQuestion("q3", AnswerStatus.NotFound, "อีกคำถามที่ไม่พบ");

        _service.Save("session-1", completedAllSlides: true, lastSlideObjectId: "slide-6");

        var summary = Assert.Single(_summaries.Items);
        Assert.Equal(2, summary.UnansweredPoints.Count);
        Assert.Contains("ถามเรื่องที่ไม่มีในบทเรียน", summary.UnansweredPoints);
        Assert.True(summary.CompletedAllSlides);
    }

    [Fact]
    public void Save_Twice_UpdatesInPlace_NotDuplicates()
    {
        AddQuestion("q1", AnswerStatus.NotFound, "ครั้งแรก");
        _service.Save("session-1", true, "slide-1");

        _service.Save("session-1", false, "slide-3");

        var summary = Assert.Single(_summaries.Items);   // updated, not duplicated
        Assert.False(summary.CompletedAllSlides);
        Assert.Equal("slide-3", summary.LastSlideObjectId);
    }

    [Fact]
    public void GetBySessionId_ReturnsNull_WhenNoSummary()
        => Assert.Null(_service.GetBySessionId("session-none"));

    [Fact]
    public void GetBySessionId_ReturnsSummaryWithQuestions()
    {
        AddQuestion("q1", AnswerStatus.NotFound, "คำถาม");
        _service.Save("session-1", true, "slide-1");

        var vm = _service.GetBySessionId("session-1");

        Assert.NotNull(vm);
        Assert.Single(vm!.Questions);
        Assert.Single(vm.UnansweredPoints);
    }
}

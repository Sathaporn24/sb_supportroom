using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Tests;

public class DocumentResourceServiceTests
{
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DocumentResourceService _service;

    public DocumentResourceServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<IDocumentResourceRepository>(_documents)
            .Register<ILessonConfigRepository>(_lessons);
        _service = new DocumentResourceService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IDocumentResourceService>.Instance,
            new LocalDocumentStorageProvider(NullLogger<LocalDocumentStorageProvider>.Instance),
            new BackgroundTaskQueue()); // real, dependency-free class - safe to use directly in tests
    }

    private DocumentResource SeedDocument(string id = "doc-1", string? lessonId = null)
    {
        var doc = new DocumentResource
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            LessonId = lessonId,
            FileName = "manual.pdf",
            ContentType = "application/pdf",
            SizeBytes = 10,
            ObsBucket = "mock-bucket",
            ObsKey = $"documents/{id}/manual.pdf",
            IndexingStatus = "indexed",
        };
        _documents.Items.Add(doc);
        return doc;
    }

    private void SeedLessonUsingPdf(string slug, string pdfDocumentResourceId)
        => _lessons.Items.Add(new LessonConfig
        {
            Id = $"lesson-{slug}",
            CompanyId = TestFixtures.CompanyId,
            Slug = slug,
            Title = "บทเรียน PDF",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.Pdf,
            PdfDocumentResourceId = pdfDocumentResourceId,
            IntroWaitMs = 5000,
            BreathPauseMs = 500,
            FinalQuestionWaitMs = 5000,
            SlideConfigs = [],
            IsActive = true,
        });

    [Fact]
    public async Task DeleteAsync_ThrowsNotFound_WhenDocumentMissing()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.DeleteAsync("ghost"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_IsBlocked_WhenDocumentIsALessonsPdfSource()
    {
        SeedDocument("doc-1");
        SeedLessonUsingPdf("login-pdf", "doc-1");

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.DeleteAsync("doc-1"));
        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Single(_documents.Items); // still there - the guard ran before any delete
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheDocument_WhenNotReferenced()
    {
        SeedDocument("doc-1");

        await _service.DeleteAsync("doc-1");

        Assert.Empty(_documents.Items);
        Assert.Equal(1, _unitOfWork.CommitCount);
    }

    [Fact]
    public void GetByLessonSlug_ThrowsNotFound_WhenLessonMissing()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(() => _service.GetByLessonSlug("ghost-lesson"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void GetStandalone_ReturnsOnlyDocumentsWithNoLesson()
    {
        SeedDocument("doc-standalone", lessonId: null);
        SeedDocument("doc-attached", lessonId: "lesson-x");

        var list = _service.GetStandalone();

        Assert.Single(list);
        Assert.Equal("doc-standalone", list[0].Id);
    }
}

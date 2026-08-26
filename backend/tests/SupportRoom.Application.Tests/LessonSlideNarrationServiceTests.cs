using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;

using static SupportRoom.Application.Tests.TestFixtures;

namespace SupportRoom.Application.Tests;

/// <summary>
/// NR-1/NR-2/NR-3/NR-9 - covers the resolver's merge rule and the save endpoint's "only persist a
/// row when it's an actual override" contract using a real, valid PDF fixture (sample.pdf, 10
/// pages) so PreviewPdfAsync exercises the real PdfSlidesRenderer parse instead of a stub.
/// </summary>
public class LessonSlideNarrationServiceTests
{
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeKnowledgeCategoryRepository _categories = new();
    private readonly FakeLessonSlideNarrationRepository _narrations = new();
    private readonly FakeLessonExcludedSlideRepository _excludedSlides = new();
    private readonly FakeBackgroundJobRepository _jobs = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly LocalDocumentStorageProvider _storage = new(NullLogger<LocalDocumentStorageProvider>.Instance);
    private readonly LessonConfigService _lessonConfigService;
    private readonly LessonSlideNarrationService _service;

    public LessonSlideNarrationServiceTests()
    {
        MapsterConfig.Apply();

        _unitOfWork.Register<ILessonConfigRepository>(_lessons);
        _unitOfWork.Register<IDocumentResourceRepository>(_documents);
        _unitOfWork.Register<IKnowledgeCategoryRepository>(_categories);
        _unitOfWork.Register<ILessonSlideNarrationRepository>(_narrations);
        _unitOfWork.Register<ILessonExcludedSlideRepository>(_excludedSlides);
        _unitOfWork.Register<IBackgroundJobRepository>(_jobs);
        _unitOfWork.Register<ICompanyRepository>(new FakeCompanyRepository());

        var resolver = new LessonSlideNarrationResolver(_unitOfWork);
        var (guard, currentUser) = TestFixtures.AdminContext(AdminRole.Owner, TestFixtures.CompanyId);
        _lessonConfigService = new LessonConfigService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            new FakeKnowledgeIndexingService(),
            _storage,
            new MemoryCache(new MemoryCacheOptions()),
            resolver,
            guard,
            currentUser);

        _service = new LessonSlideNarrationService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<ILessonSlideNarrationService>.Instance,
            _lessonConfigService,
            resolver);
    }

    private async Task<LessonConfig> SeedPdfLessonAsync(string lessonId = "lesson-1", string documentId = "doc-narr-1")
    {
        var pdfBytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.pdf"));
        var document = new DocumentResource
        {
            Id = documentId,
            CompanyId = TestFixtures.CompanyId,
            ScopeType = KnowledgeScopeType.Lesson,
            ScopeId = lessonId,
            FileName = "manual.pdf",
            ContentType = "application/pdf",
            SizeBytes = pdfBytes.Length,
            ObsBucket = "mock-bucket",
            ObsKey = $"documents/{documentId}/manual.pdf",
            IndexingStatus = "indexed",
        };
        _documents.Items.Add(document);
        using var stream = new MemoryStream(pdfBytes);
        await _storage.UploadAsync(document.ObsKey, stream, "application/pdf");

        var lesson = new LessonConfig
        {
            Id = lessonId,
            CompanyId = TestFixtures.CompanyId,
            Slug = lessonId,
            CategoryId = "kbcat-child",
            Title = "บทเรียนทดสอบ",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.Pdf,
            PdfDocumentResourceId = documentId,
            SlideConfigs = [],
            IsActive = true,
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    private LessonConfig SeedGoogleSlidesLesson(string lessonId = "lesson-gs")
    {
        var lesson = new LessonConfig
        {
            Id = lessonId,
            CompanyId = TestFixtures.CompanyId,
            Slug = lessonId,
            CategoryId = "kbcat-child",
            Title = "บทเรียน Google Slides",
            SlidesSourceUrl = "https://docs.google.com/presentation/d/abc/edit",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            PresentationId = "abc",
            SlideConfigs = [],
            IsActive = true,
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    // ---- NR-9 ---------------------------------------------------------

    [Fact]
    public async Task SaveAsync_RejectsGoogleSlidesLessons_AtTheServer()
    {
        SeedGoogleSlidesLesson();

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.SaveAsync("lesson-gs", "slide-1", "บทพูดใหม่"));
        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_narrations.Items);
    }

    [Fact]
    public async Task GetAllAsync_RejectsGoogleSlidesLessons()
    {
        SeedGoogleSlidesLesson();

        await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.GetAllAsync("lesson-gs"));
    }

    // ---- NR-2 -----------------------------------------------------------

    [Fact]
    public async Task SaveAsync_WithDifferentText_UpsertsARow()
    {
        await SeedPdfLessonAsync();

        await _service.SaveAsync("lesson-1", "pdf-page-1", "บทพูดที่ CS แก้เอง");

        var row = Assert.Single(_narrations.Items);
        Assert.Equal("บทพูดที่ CS แก้เอง", row.NarrationText);
        Assert.Single(_jobs.Items); // NR-6 - re-index job queued
        Assert.Equal(BackgroundJobType.LessonIndex, _jobs.Items[0].JobType);
        Assert.Equal("lesson-1", _jobs.Items[0].TargetId);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyText_DeletesAnExistingOverride_InsteadOfSavingBlank()
    {
        var lesson = await SeedPdfLessonAsync();
        await _service.SaveAsync(lesson.Id, "pdf-page-1", "แก้ไว้ก่อนหน้านี้");
        Assert.Single(_narrations.Items);

        await _service.SaveAsync(lesson.Id, "pdf-page-1", "   ");

        Assert.All(_narrations.Items, n => Assert.True(n.IsDelete));
    }

    [Fact]
    public async Task SaveAsync_WithTextEqualToThePrefill_NeverCreatesARow()
    {
        var lesson = await SeedPdfLessonAsync();
        var content = await _lessonConfigService.PreviewPdfAsync(lesson.PdfDocumentResourceId!);
        var prefill = content.Slides.First(s => s.SlideObjectId == "pdf-page-1").SpeakerNotes;

        await _service.SaveAsync(lesson.Id, "pdf-page-1", prefill);

        Assert.Empty(_narrations.Items);
        Assert.Empty(_jobs.Items); // nothing changed -> no re-index either
    }

    [Fact]
    public async Task SaveAsync_TypingBackTheSamePrefillOverAnExistingOverride_DeletesTheRow()
    {
        var lesson = await SeedPdfLessonAsync();
        var content = await _lessonConfigService.PreviewPdfAsync(lesson.PdfDocumentResourceId!);
        var prefill = content.Slides.First(s => s.SlideObjectId == "pdf-page-1").SpeakerNotes;

        await _service.SaveAsync(lesson.Id, "pdf-page-1", "ค่าที่ CS แก้ไว้ก่อน");
        Assert.Single(_narrations.Items);

        await _service.SaveAsync(lesson.Id, "pdf-page-1", prefill);

        Assert.All(_narrations.Items, n => Assert.True(n.IsDelete));
    }

    // ---- NR-3 -------------------------------------------------------------

    [Fact]
    public async Task CountByLessonId_ReflectsOnlyLiveOverrides()
    {
        var lesson = await SeedPdfLessonAsync();
        await _service.SaveAsync(lesson.Id, "pdf-page-1", "แก้หน้า 1");
        await _service.SaveAsync(lesson.Id, "pdf-page-2", "แก้หน้า 2");

        Assert.Equal(2, _service.CountByLessonId(lesson.Id).Count);
    }

    [Fact]
    public async Task CountByLessonId_AlsoReportsExcludedCount()
    {
        var lesson = await SeedPdfLessonAsync();
        _excludedSlides.Items.Add(new LessonExcludedSlide
        {
            Id = "exsl-1",
            CompanyId = TestFixtures.CompanyId,
            LessonId = lesson.Id,
            SlideObjectId = "pdf-page-1",
            CreateDate = DateTime.UtcNow,
        });

        var (count, excludedCount) = _service.CountByLessonId(lesson.Id);

        Assert.Equal(0, count);
        Assert.Equal(1, excludedCount);
    }

    // ---- Resolver (NR-1) ---------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ResolvesOverriddenText_AndFlagsWhichPagesAreOverridden()
    {
        var lesson = await SeedPdfLessonAsync();
        await _service.SaveAsync(lesson.Id, "pdf-page-1", "บทพูดที่ CS แก้เอง");

        var result = await _service.GetAllAsync(lesson.Id);

        var page1 = result.Slides.Single(s => s.SlideObjectId == "pdf-page-1");
        Assert.True(page1.IsOverridden);
        Assert.Equal("บทพูดที่ CS แก้เอง", page1.NarrationText);

        var page2 = result.Slides.Single(s => s.SlideObjectId == "pdf-page-2");
        Assert.False(page2.IsOverridden);
    }
}

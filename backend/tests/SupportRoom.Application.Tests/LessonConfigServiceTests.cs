using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
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

public class LessonConfigServiceTests
{
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeKnowledgeIndexingService _knowledge = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly LocalDocumentStorageProvider _storage = new(NullLogger<LocalDocumentStorageProvider>.Instance);
    private readonly LessonConfigService _service;

    public LessonConfigServiceTests()
    {
        // SaveAsync/GetBySlug end with .Adapt<LessonConfigViewModel>(), which relies on the
        // production Mapster rules (e.g. CreateDate -> CreatedAt string).
        MapsterConfig.Apply();

        _unitOfWork.Register<ILessonConfigRepository>(_lessons);
        _unitOfWork.Register<IDocumentResourceRepository>(_documents);

        _service = new LessonConfigService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            _knowledge,
            _storage,
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static LessonConfigDto NewDto(
        string slug = "lesson-a",
        string contentSourceType = LessonContentSourceType.GoogleSlides,
        string slidesSourceUrl = "",
        string? pdfDocumentResourceId = null,
        bool isActive = true) => new()
    {
        Slug = slug,
        Title = "บทเรียนทดสอบ",
        Description = null,
        SlidesSourceUrl = slidesSourceUrl,
        SlidesEmbedUrl = null,
        ContentSourceType = contentSourceType,
        PdfDocumentResourceId = pdfDocumentResourceId,
        IntroWaitMs = 5000,
        BreathPauseMs = 500,
        FinalQuestionWaitMs = 5000,
        SlideConfigs = [],
        IsActive = isActive,
    };

    private DocumentResource SeedPdfDocument(string id = "doc-1", string? lessonId = null)
    {
        var doc = new DocumentResource
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            LessonId = lessonId,
            FileName = "manual.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1234,
            ObsBucket = "mock-bucket",
            ObsKey = $"documents/{id}/manual.pdf",
            IndexingStatus = "indexed",
        };
        _documents.Items.Add(doc);
        return doc;
    }

    /// <summary>Seeds a DB row AND writes a real, valid PDF's bytes to the same storage provider
    /// _service reads from - unlike SeedPdfDocument alone, this lets RenderPdfPageAsync/
    /// GetTeachingContentBySlugAsync actually succeed instead of hitting the "file missing"
    /// path, needed for the caching tests below.</summary>
    private async Task<DocumentResource> SeedRealPdfBytesAsync(string id)
    {
        var doc = SeedPdfDocument(id);
        var pdfBytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.pdf"));
        using var stream = new MemoryStream(pdfBytes);
        await _storage.UploadAsync(doc.ObsKey, stream, "application/pdf");
        return doc;
    }

    // ---- SaveAsync ---------------------------------------------------------

    [Fact]
    public async Task SaveAsync_RejectsAnUnknownContentSourceType()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.SaveAsync(NewDto(contentSourceType: "pdf-v2")));
        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_lessons.Items); // nothing persisted on a validation failure
    }

    [Fact]
    public async Task SaveAsync_CreatesANewLesson_AndCommitsOnce()
    {
        await _service.SaveAsync(NewDto(slug: "brand-new"));

        var saved = Assert.Single(_lessons.Items);
        Assert.Equal("brand-new", saved.Slug);
        Assert.Equal(1, _unitOfWork.CommitCount);
    }

    [Fact]
    public async Task SaveAsync_IsAnUpsertBySlug_NotADuplicate()
    {
        await _service.SaveAsync(NewDto(slug: "same-slug"));
        await _service.SaveAsync(NewDto(slug: "same-slug"));

        Assert.Single(_lessons.Items); // second save updates, doesn't add a second row
    }

    [Fact]
    public async Task SaveAsync_ForPdf_NullsPresentationId_AndKeepsTheDocumentPointer()
    {
        var result = await _service.SaveAsync(
            NewDto(contentSourceType: LessonContentSourceType.Pdf, pdfDocumentResourceId: "doc-1"));

        var saved = Assert.Single(_lessons.Items);
        Assert.Equal(LessonContentSourceType.Pdf, saved.ContentSourceType);
        Assert.Null(saved.PresentationId);           // a PDF lesson must not carry a stale Slides id
        Assert.Equal("doc-1", saved.PdfDocumentResourceId);
        Assert.Null(result.PresentationId);
    }

    [Fact]
    public async Task SaveAsync_ForGoogleSlidesWithAUrl_ResolvesPresentationIdViaTheProvider()
    {
        // Real GoogleSlidesProvider hits the live API to confirm access, so this needs an
        // actually-reachable presentation (see TestFixtures.GooglePresentationId).
        await _service.SaveAsync(NewDto(
            contentSourceType: LessonContentSourceType.GoogleSlides,
            slidesSourceUrl: TestGoogleSlidesUrl));

        var saved = Assert.Single(_lessons.Items);
        Assert.Equal(GooglePresentationId, saved.PresentationId);
        Assert.Equal(1, _knowledge.IndexLessonCallCount); // re-indexed because presentationId is set
    }

    // ---- GetBySlug --------------------------------------------------------

    [Fact]
    public void GetBySlug_ThrowsNotFound_WhenMissing()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(() => _service.GetBySlug("nope"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    // ---- GetTeachingContentBySlugAsync ------------------------------------

    [Fact]
    public async Task GetTeachingContent_ThrowsNotFound_WhenLessonInactive()
    {
        await _service.SaveAsync(NewDto(slug: "hidden", isActive: false));

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.GetTeachingContentBySlugAsync("hidden"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task GetTeachingContent_ForPdfWithoutADocument_ThrowsConfigError()
    {
        await _service.SaveAsync(NewDto(slug: "pdf-empty", contentSourceType: LessonContentSourceType.Pdf, pdfDocumentResourceId: null));

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.GetTeachingContentBySlugAsync("pdf-empty"));
        // Codebase convention: an unfinished lesson setup is a ConfigError (500), matching the
        // Google-Slides branch's "PresentationId not set" behavior. Locking in the current contract.
        Assert.Equal(ApiErrorCode.ConfigError, ex.Code);
    }

    [Fact]
    public async Task GetTeachingContent_ForGoogleSlides_ReturnsResolvedSlides()
    {
        await _service.SaveAsync(NewDto(
            slug: "google-ok",
            contentSourceType: LessonContentSourceType.GoogleSlides,
            slidesSourceUrl: TestGoogleSlidesUrl));

        var content = await _service.GetTeachingContentBySlugAsync("google-ok");

        Assert.NotEmpty(content.Slides);                       // real deck has 5 slides
        Assert.Equal("google-ok", content.Lesson.Slug);
        Assert.All(content.Slides, s => Assert.False(string.IsNullOrWhiteSpace(s.SlideObjectId)));
    }

    // ---- RenderPdfPageAsync guards (bug fix regression) -------------------

    [Fact]
    public async Task RenderPdfPage_RejectsAPageNumberBelowOne()
    {
        SeedPdfDocument("doc-1");

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.RenderPdfPageAsync("doc-1", 0));
        Assert.Equal(400, (int)ex.StatusCode); // used to fall through to a 500 from PDFium
    }

    [Fact]
    public async Task RenderPdfPage_ThrowsNotFound_WhenDocumentMissing()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.RenderPdfPageAsync("does-not-exist", 1));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task RenderPdfPage_ThrowsNotFound_WhenStoredFileIsMissingFromStorage()
    {
        // The DB row exists but its bytes were never (or no longer) in the storage provider - a
        // manual deletion or a storage reset without a matching migration. Used to leak the
        // provider's raw FileNotFoundException as an opaque 500 instead of the same clean 404 a
        // missing DB row already gets.
        SeedPdfDocument("doc-1"); // DB row exists, but its bytes were never actually written to disk

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.RenderPdfPageAsync("doc-1", 1));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    // ---- PDF content/image caching (perf fix regression) -------------------

    [Fact]
    public async Task GetTeachingContent_ForPdf_ReturnsEquivalentContent_OnRepeatCalls()
    {
        await SeedRealPdfBytesAsync("doc-cache-1");
        await _service.SaveAsync(NewDto(
            slug: "pdf-cached", contentSourceType: LessonContentSourceType.Pdf, pdfDocumentResourceId: "doc-cache-1"));

        var first = await _service.GetTeachingContentBySlugAsync("pdf-cached");
        var second = await _service.GetTeachingContentBySlugAsync("pdf-cached");

        // BuildPdfContentAsync is now cached (see ILessonConfigService.cs) - the second call must
        // still return the same content as the first, not something corrupted/stale/truncated.
        Assert.Equal(first.Slides.Count, second.Slides.Count);
        Assert.Equal(first.Slides.Select(s => s.SlideObjectId), second.Slides.Select(s => s.SlideObjectId));
        Assert.Equal(first.Slides.Select(s => s.SpeakerNotes), second.Slides.Select(s => s.SpeakerNotes));
    }

    [Fact]
    public async Task RenderPdfPage_ReturnsByteIdenticalPng_OnRepeatCalls()
    {
        await SeedRealPdfBytesAsync("doc-cache-2");

        var first = await _service.RenderPdfPageAsync("doc-cache-2", 1);
        var second = await _service.RenderPdfPageAsync("doc-cache-2", 1);

        // Rendered PNG bytes are now cached (see ILessonConfigService.cs) - must come back
        // byte-for-byte identical, not just "visually the same."
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task RenderPdfPage_UsesCachedContentForPageCount_NotASeparatePdfiumParse()
    {
        // Page count now comes from BuildPdfContentAsync's cached Slides.Count instead of a
        // separate PdfSlidesRenderer.GetPageCount PDFium parse - the fixture PDF has 10 real
        // pages (verified via PdfPig directly), so page 10 must render and page 11 must be
        // rejected as out of range.
        var doc = await SeedRealPdfBytesAsync("doc-cache-3");

        var lastPage = await _service.RenderPdfPageAsync(doc.Id, 10);
        Assert.NotEmpty(lastPage);

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.RenderPdfPageAsync(doc.Id, 11));
        Assert.Equal(404, (int)ex.StatusCode);
        Assert.Contains("10 หน้า", ex.Message);
    }
}

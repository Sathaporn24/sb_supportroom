using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Common;
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
    private readonly FakeKnowledgeCategoryRepository _categories = new();
    private readonly FakeLessonSlideNarrationRepository _narrations = new();
    private readonly FakeLessonExcludedSlideRepository _excludedSlides = new();
    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeTrainingLinkRepository _links = new();
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
        _unitOfWork.Register<IKnowledgeCategoryRepository>(_categories);
        _unitOfWork.Register<ILessonSlideNarrationRepository>(_narrations);
        _unitOfWork.Register<ILessonExcludedSlideRepository>(_excludedSlides);
        _unitOfWork.Register<ICompanyRepository>(_companies);
        _unitOfWork.Register<ITrainingLinkRepository>(_links);
        _unitOfWork.Register<ILearningSessionRepository>(new FakeLearningSessionRepository());
        _categories.Items.Add(new KnowledgeCategory
        {
            Id = "kbcat-child",
            CompanyId = TestFixtures.CompanyId,
            ParentId = "kbcat-parent",
            Level = 2,
            Name = "หมวดย่อย",
            SortOrder = 0,
            IsSystemDefault = false,
        });

        var serviceProvider = new FakeServiceProvider();
        serviceProvider.Register<ITrainingLinkService>(
            new TrainingLinkService(_unitOfWork, serviceProvider, NullLogger<ITrainingLinkService>.Instance));

        var (guard, currentUser) = TestFixtures.AdminContext(AdminRole.Owner, TestFixtures.CompanyId);
        _service = new LessonConfigService(
            _unitOfWork,
            serviceProvider,
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            _knowledge,
            _storage,
            new MemoryCache(new MemoryCacheOptions()),
            new LessonSlideNarrationResolver(_unitOfWork),
            guard,
            currentUser);
    }

    private static LessonConfigDto NewDto(
        string slug = "lesson-a",
        string contentSourceType = LessonContentSourceType.GoogleSlides,
        string slidesSourceUrl = "",
        string? pdfDocumentResourceId = null,
        bool isActive = true,
        List<string>? excludedSlideObjectIds = null) => new()
    {
        Slug = slug,
        CategoryId = "kbcat-child",
        Title = "บทเรียนทดสอบ",
        Description = null,
        SlidesSourceUrl = slidesSourceUrl,
        SlidesEmbedUrl = null,
        ContentSourceType = contentSourceType,
        PdfDocumentResourceId = pdfDocumentResourceId,
        SlideConfigs = [],
        IsActive = isActive,
        ExcludedSlideObjectIds = excludedSlideObjectIds,
    };

    private DocumentResource SeedPdfDocument(string id = "doc-1", string? lessonId = null)
    {
        var doc = new DocumentResource
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            ScopeType = lessonId is null ? "company" : "lesson",
            ScopeId = lessonId,
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
    [Trait(TestCategories.Category, TestCategories.Integration)]
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
    [Trait(TestCategories.Category, TestCategories.Integration)]
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

    // ---- GetTeachingContentByLinkAsync (LP-1/LP-4) ------------------------

    /// <summary>LP-1/LP-4/LP-14.1 (2026-08-22 rewrite) - pacing has no per-lesson override anymore;
    /// this is the ONE place it's read, straight off Company.Default*Ms with no merge/resolver in
    /// between. The three constants below are deliberately not 5000/500/5000 (ServerDefaults) so
    /// this test can't pass by coincidence - it only passes if the values genuinely came from this
    /// specific Company row.</summary>
    [Fact]
    public async Task GetTeachingContentByLink_ReturnsPacingFromCompany_NotFromTheLesson()
    {
        var doc = await SeedRealPdfBytesAsync("doc-link-1");
        await _service.SaveAsync(NewDto(
            slug: "linked-lesson", contentSourceType: LessonContentSourceType.Pdf, pdfDocumentResourceId: doc.Id));
        var lesson = _lessons.Items.Single(l => l.Slug == "linked-lesson");
        _companies.Items.Add(new Company
        {
            Id = TestFixtures.CompanyId,
            Name = "Test Company",
            IsActive = true,
            DefaultIntroWaitMs = 1234,
            DefaultBreathPauseMs = 222,
            DefaultFinalQuestionWaitMs = 3333,
        });
        _links.Items.Add(new TrainingLink
        {
            Id = "link-1",
            CompanyId = TestFixtures.CompanyId,
            Token = "tok-1",
            LessonId = lesson.Id,
            LessonSlug = lesson.Slug,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });

        var content = await _service.GetTeachingContentByLinkAsync("tok-1", null);

        Assert.Equal(1234, content.Lesson.IntroWaitMs);
        Assert.Equal(222, content.Lesson.BreathPauseMs);
        Assert.Equal(3333, content.Lesson.FinalQuestionWaitMs);
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

    // ---- PDF preview session (NR-10/NR-11/NR-5) ----------------------------

    private static Task<Stream> OpenSamplePdfAsync()
        => Task.FromResult<Stream>(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.pdf")));

    [Fact]
    public async Task CreatePdfPreviewSession_ReturnsSlidesShapedLikeNarrationViewModel_WithoutPersistingAnything()
    {
        using var stream = await OpenSamplePdfAsync();

        var session = await _service.CreatePdfPreviewSessionAsync(stream, "manual.pdf");

        Assert.NotEmpty(session.PreviewId);
        Assert.Equal(10, session.PageCount);
        Assert.Equal(10, session.Slides.Count);
        Assert.False(session.IsLikelyScanned); // fixture has real extracted text
        Assert.All(session.Slides, s => Assert.False(string.IsNullOrWhiteSpace(s.SlideObjectId)));
        // NR-10 - nothing written to any of the DB-backed fakes.
        Assert.Empty(_documents.Items);
        Assert.Empty(_lessons.Items);
        Assert.Equal(0, _unitOfWork.CommitCount);
    }

    [Fact]
    public async Task RenderPdfPreviewPage_ReturnsAnImage_ForTheSameCompanyThatCreatedTheSession()
    {
        using var stream = await OpenSamplePdfAsync();
        var session = await _service.CreatePdfPreviewSessionAsync(stream, "manual.pdf");

        var png = await _service.RenderPdfPreviewPageAsync(session.PreviewId, 1);

        Assert.NotEmpty(png);
    }

    [Fact]
    public async Task RenderPdfPreviewPage_ThrowsNotFound_WhenPreviewIdNeverExisted()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.RenderPdfPreviewPageAsync("pdfprev-does-not-exist", 1));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    /// <summary>NR-11 - the security-load-bearing test: a previewId that belongs to another
    /// company must be rejected with the exact same NotFound message as a previewId that never
    /// existed, so a caller cannot tell "not yours" apart from "doesn't exist".</summary>
    [Fact]
    public async Task RenderPdfPreviewPage_ThrowsTheSameNotFound_ForWrongCompany_AsForAMissingSession()
    {
        // Both services below must share one IMemoryCache instance - a preview session created
        // under one company's own cache wouldn't prove anything about cross-company isolation.
        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        var (ownerGuard, ownerCurrentUser) = TestFixtures.AdminContext(AdminRole.Owner, TestFixtures.CompanyId);
        var ownerService = new LessonConfigService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            _knowledge,
            _storage,
            sharedCache,
            new LessonSlideNarrationResolver(_unitOfWork),
            ownerGuard,
            ownerCurrentUser);
        var otherReaderServiceProvider = new FakeServiceProvider();
        var otherContext = new CompanyContext();
        otherContext.Resolve(TestFixtures.OtherCompanyId);
        var (otherGuard, otherCurrentUser) = TestFixtures.AdminContext(AdminRole.Owner, TestFixtures.OtherCompanyId);
        var otherReaderService = new LessonConfigService(
            _unitOfWork,
            otherReaderServiceProvider.Register<ICompanyContext>(otherContext),
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            _knowledge,
            _storage,
            sharedCache,
            new LessonSlideNarrationResolver(_unitOfWork),
            otherGuard,
            otherCurrentUser);

        using var stream = await OpenSamplePdfAsync();
        var session = await ownerService.CreatePdfPreviewSessionAsync(stream, "manual.pdf");

        var wrongCompanyEx = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => otherReaderService.RenderPdfPreviewPageAsync(session.PreviewId, 1));
        var missingSessionEx = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => otherReaderService.RenderPdfPreviewPageAsync("pdfprev-does-not-exist", 1));

        Assert.Equal(404, (int)wrongCompanyEx.StatusCode);
        Assert.Equal(missingSessionEx.Message, wrongCompanyEx.Message);
    }

    [Fact]
    public async Task RenderPdfPreviewPage_RejectsAPageNumberBelowOne()
    {
        using var stream = await OpenSamplePdfAsync();
        var session = await _service.CreatePdfPreviewSessionAsync(stream, "manual.pdf");

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.RenderPdfPreviewPageAsync(session.PreviewId, 0));
        Assert.Equal(400, (int)ex.StatusCode);
    }

    [Fact]
    public async Task CreatePdfPreviewSession_RejectsANonPdfFile()
    {
        using var stream = new MemoryStream("not a pdf"u8.ToArray());

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.CreatePdfPreviewSessionAsync(stream, "notes.txt"));
        Assert.Equal(400, (int)ex.StatusCode);
    }

    // ---- EX-9 (excludedSlideObjectIds on SaveAsync) ------------------------

    [Fact]
    public async Task SaveAsync_WithExcludedSlideObjectIds_WritesTheSetInTheSameRequestThatTriggersNR3Clear()
    {
        // EX-9's mandated (ก)(ข)(ค) ordering - the exclusion set written in THIS request must
        // survive even though the very same request also triggers NR-3's "PdfDocumentResourceId
        // changed" clear. Swapping (ก)/(ข) would make this new set vanish silently.
        var docA = await SeedRealPdfBytesAsync("doc-order-a");
        await _service.SaveAsync(NewDto(
            slug: "order-lesson", contentSourceType: LessonContentSourceType.Pdf, pdfDocumentResourceId: docA.Id));

        var docB = await SeedRealPdfBytesAsync("doc-order-b");
        await _service.SaveAsync(NewDto(
            slug: "order-lesson",
            contentSourceType: LessonContentSourceType.Pdf,
            pdfDocumentResourceId: docB.Id,
            excludedSlideObjectIds: ["pdf-page-1"]));

        var lesson = _lessons.Items.Single(l => l.Slug == "order-lesson");
        var liveExclusions = _excludedSlides.Items.Where(x => x.LessonId == lesson.Id && !x.IsDelete).ToList();
        Assert.Single(liveExclusions);
        Assert.Equal("pdf-page-1", liveExclusions[0].SlideObjectId);
    }

    [Fact]
    public async Task SaveAsync_WithExcludedSlideObjectIds_Null_LeavesExistingExclusionsUntouched()
    {
        var doc = await SeedRealPdfBytesAsync("doc-null-excl");
        await _service.SaveAsync(NewDto(
            slug: "untouched-lesson",
            contentSourceType: LessonContentSourceType.Pdf,
            pdfDocumentResourceId: doc.Id,
            excludedSlideObjectIds: ["pdf-page-1"]));
        var lesson = _lessons.Items.Single(l => l.Slug == "untouched-lesson");

        // Same PdfDocumentResourceId (no NR-3 trigger), ExcludedSlideObjectIds omitted entirely.
        await _service.SaveAsync(NewDto(
            slug: "untouched-lesson", contentSourceType: LessonContentSourceType.Pdf, pdfDocumentResourceId: doc.Id));

        var liveExclusions = _excludedSlides.Items.Where(x => x.LessonId == lesson.Id && !x.IsDelete).ToList();
        Assert.Single(liveExclusions);
    }

    [Fact]
    public async Task SaveAsync_WithExcludedSlideObjectIds_RejectsCuttingEveryPage()
    {
        var doc = await SeedRealPdfBytesAsync("doc-floor");
        var allTenPages = Enumerable.Range(1, 10).Select(n => $"pdf-page-{n}").ToList();

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.SaveAsync(NewDto(
            slug: "floor-lesson",
            contentSourceType: LessonContentSourceType.Pdf,
            pdfDocumentResourceId: doc.Id,
            excludedSlideObjectIds: allTenPages)));

        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Contains("อย่างน้อย 1 หน้า", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_WithExcludedSlideObjectIds_RejectsAPageThatDoesNotExistInTheDeck()
    {
        var doc = await SeedRealPdfBytesAsync("doc-invalid-page");

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.SaveAsync(NewDto(
            slug: "invalid-page-lesson",
            contentSourceType: LessonContentSourceType.Pdf,
            pdfDocumentResourceId: doc.Id,
            excludedSlideObjectIds: ["pdf-page-999"])));

        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task SaveAsync_WithALegacyDuplicateExcludedSlideRow_CleansUpTheDuplicateInsteadOfThrowing()
    {
        // P11-01 (2nd re-check) - seeds a duplicate (LessonId, SlideObjectId) pair the way the
        // pre-fix bug actually left it: two live rows for the same page, written directly to the
        // fake's backing store rather than through ToggleAsync/SaveAsync (which no longer produce
        // this state on a clean DB). ApplyExcludedSlidesAsync's reconciliation must hard-delete the
        // sibling even though this save's excludedSlideObjectIds never mentions "pdf-page-2" - a
        // page nobody is touching in this particular save is exactly the case the previous fix
        // (which only grouped, never deleted) still left broken.
        var doc = await SeedRealPdfBytesAsync("doc-legacy-dupe");
        await _service.SaveAsync(NewDto(
            slug: "legacy-dupe-lesson",
            contentSourceType: LessonContentSourceType.Pdf,
            pdfDocumentResourceId: doc.Id,
            excludedSlideObjectIds: ["pdf-page-1"]));
        var lesson = _lessons.Items.Single(l => l.Slug == "legacy-dupe-lesson");

        _excludedSlides.Items.Add(new LessonExcludedSlide
        {
            Id = "exsl-legacy-dupe-1",
            CompanyId = TestFixtures.CompanyId,
            LessonId = lesson.Id,
            SlideObjectId = "pdf-page-2",
            CreateDate = DateTime.UtcNow.AddDays(-2),
            IsDelete = false,
        });
        _excludedSlides.Items.Add(new LessonExcludedSlide
        {
            Id = "exsl-legacy-dupe-2",
            CompanyId = TestFixtures.CompanyId,
            LessonId = lesson.Id,
            SlideObjectId = "pdf-page-2",
            CreateDate = DateTime.UtcNow.AddDays(-1),
            IsDelete = false,
        });

        // Same excludedSlideObjectIds as before ("pdf-page-2" not mentioned) - the duplicate on
        // pdf-page-2 must still get cleaned up as a side effect of any save touching this lesson.
        await _service.SaveAsync(NewDto(
            slug: "legacy-dupe-lesson",
            contentSourceType: LessonContentSourceType.Pdf,
            pdfDocumentResourceId: doc.Id,
            excludedSlideObjectIds: ["pdf-page-1"]));

        var page2Rows = _excludedSlides.Items.Where(x => x.LessonId == lesson.Id && x.SlideObjectId == "pdf-page-2").ToList();
        var singleSurvivor = Assert.Single(page2Rows); // the duplicate sibling was hard-deleted, not just soft-deleted alongside it
        Assert.Equal("exsl-legacy-dupe-2", singleSurvivor.Id); // most-recently-touched of the two live rows wins
        // page-2 was never in this save's set, so the surviving row ends up soft-deleted -
        // GetOne must still return it (not throw) since it deliberately includes soft-deleted rows.
        Assert.Equal(singleSurvivor, _excludedSlides.GetOne(lesson.Id, "pdf-page-2"));
        Assert.True(singleSurvivor.IsDelete);
    }
}

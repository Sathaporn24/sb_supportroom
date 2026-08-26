using System.Text;
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
/// EX-4/EX-5/EX-6/EX-8/EX-12(ข) - the toggle endpoint's business logic, using the real, valid
/// sample.pdf fixture (10 pages) so PreviewPdfAsync exercises the real PdfSlidesRenderer parse.
/// </summary>
public class LessonExcludedSlideServiceTests
{
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeLessonExcludedSlideRepository _excludedSlides = new();
    private readonly FakeDocumentChunkRepository _documentChunks = new();
    private readonly FakeBackgroundJobRepository _jobs = new();
    private readonly FakeKnowledgeIndexingService _knowledge = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly LocalDocumentStorageProvider _storage = new(NullLogger<LocalDocumentStorageProvider>.Instance);
    private readonly LessonConfigService _lessonConfigService;
    private readonly LessonExcludedSlideService _service;

    public LessonExcludedSlideServiceTests()
    {
        MapsterConfig.Apply();

        _unitOfWork.Register<ILessonConfigRepository>(_lessons);
        _unitOfWork.Register<IDocumentResourceRepository>(_documents);
        _unitOfWork.Register<IKnowledgeCategoryRepository>(new FakeKnowledgeCategoryRepository());
        _unitOfWork.Register<ILessonSlideNarrationRepository>(new FakeLessonSlideNarrationRepository());
        _unitOfWork.Register<ILessonExcludedSlideRepository>(_excludedSlides);
        _unitOfWork.Register<IDocumentChunkRepository>(_documentChunks);
        _unitOfWork.Register<IBackgroundJobRepository>(_jobs);
        _unitOfWork.Register<ICompanyRepository>(new FakeCompanyRepository());

        var resolver = new LessonSlideNarrationResolver(_unitOfWork);
        var (guard, currentUser) = TestFixtures.AdminContext(AdminRole.Owner, TestFixtures.CompanyId);
        _lessonConfigService = new LessonConfigService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<ILessonConfigService>.Instance,
            new GoogleSlidesProvider(NullLogger<GoogleSlidesProvider>.Instance),
            _knowledge,
            _storage,
            new MemoryCache(new MemoryCacheOptions()),
            resolver,
            guard,
            currentUser);

        _service = new LessonExcludedSlideService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<ILessonExcludedSlideService>.Instance,
            _lessonConfigService,
            _knowledge);
    }

    private async Task<LessonConfig> SeedPdfLessonAsync(string lessonId = "lesson-1", string documentId = "doc-exsl-1")
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

    /// <summary>P11-03 - a lesson whose own document genuinely has only 1 page, distinct from the
    /// shared 10-page sample.pdf fixture every other test in this file uses. Needed to prove
    /// EX-12(ข)'s scoping for real: two lessons built off the *same* fixture would both consider
    /// "pdf-page-5" valid regardless of whether the check is actually scoped per-lesson.</summary>
    private async Task<LessonConfig> SeedSinglePagePdfLessonAsync(string lessonId, string documentId)
    {
        var pdfBytes = BuildMinimalPdfBytes(pageCount: 1);
        var document = new DocumentResource
        {
            Id = documentId,
            CompanyId = TestFixtures.CompanyId,
            ScopeType = KnowledgeScopeType.Lesson,
            ScopeId = lessonId,
            FileName = "manual-small.pdf",
            ContentType = "application/pdf",
            SizeBytes = pdfBytes.Length,
            ObsBucket = "mock-bucket",
            ObsKey = $"documents/{documentId}/manual-small.pdf",
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
            Title = "บทเรียนทดสอบ (เล็ก)",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.Pdf,
            PdfDocumentResourceId = documentId,
            SlideConfigs = [],
            IsActive = true,
        };
        _lessons.Items.Add(lesson);
        return lesson;
    }

    /// <summary>Hand-built, minimally valid PDF (no real xref table - PdfPig falls back to its
    /// brute-force object scan/recovery to parse it) with exactly <paramref name="pageCount"/>
    /// blank pages. Only used to give a lesson a genuinely different page count than the shared
    /// sample.pdf fixture, for P11-03's cross-lesson id test.</summary>
    private static byte[] BuildMinimalPdfBytes(int pageCount)
    {
        var pageObjNumbers = Enumerable.Range(1, pageCount).Select(i => 2 + i).ToList();
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        sb.Append($"2 0 obj\n<< /Type /Pages /Kids [{string.Join(" ", pageObjNumbers.Select(n => $"{n} 0 R"))}] /Count {pageCount} >>\nendobj\n");
        foreach (var n in pageObjNumbers)
        {
            sb.Append($"{n} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>\nendobj\n");
        }
        sb.Append($"trailer\n<< /Size {pageObjNumbers.Count + 2} /Root 1 0 R >>\nstartxref\n0\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    // ---- EX-4 idempotency ---------------------------------------------

    [Fact]
    public async Task ToggleAsync_ExcludingTwiceInARow_IsANoOpTheSecondTime()
    {
        var lesson = await SeedPdfLessonAsync();

        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: true);
        var jobCountAfterFirst = _jobs.Items.Count;
        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: true);

        Assert.Single(_excludedSlides.Items, x => !x.IsDelete);
        Assert.Equal(jobCountAfterFirst, _jobs.Items.Count); // no second job enqueued
    }

    [Fact]
    public async Task ToggleAsync_RestoringAPageThatWasNeverExcluded_IsANoOp()
    {
        var lesson = await SeedPdfLessonAsync();

        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: false);

        Assert.Empty(_excludedSlides.Items);
        Assert.Empty(_jobs.Items);
    }

    [Fact]
    public async Task ToggleAsync_ExcludeThenRestore_UndeletesTheSameRow_InsteadOfLeavingADuplicate()
    {
        var lesson = await SeedPdfLessonAsync();

        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: true);
        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: false);

        var row = Assert.Single(_excludedSlides.Items); // one row total, never a second one
        Assert.True(row.IsDelete);
    }

    // ---- EX-8 ------------------------------------------------------------

    [Fact]
    public async Task ToggleAsync_RejectsCuttingTheLastRemainingPage()
    {
        var lesson = await SeedPdfLessonAsync();
        for (var page = 1; page <= 9; page++)
        {
            await _service.ToggleAsync(lesson.Id, $"pdf-page-{page}", excluded: true);
        }

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.ToggleAsync(lesson.Id, "pdf-page-10", excluded: true));

        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Contains("อย่างน้อย 1 หน้า", ex.Message);
        // Rejected - page 10 must still be a live (non-excluded) page, not a row that leaked
        // through before the floor check threw.
        Assert.True(_excludedSlides.GetOne(lesson.Id, "pdf-page-10") is null or { IsDelete: true });
    }

    // ---- EX-12(ข) ----------------------------------------------------------

    [Fact]
    public async Task ToggleAsync_ThrowsNotFound_ForASlideObjectIdThatDoesNotExistInTheDeck()
    {
        var lesson = await SeedPdfLessonAsync();

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.ToggleAsync(lesson.Id, "pdf-page-999", excluded: true));

        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task ToggleAsync_OnOneLesson_NeverWritesOrEnqueuesAnythingForAnotherLessonsRows()
    {
        // EX-12(ข)'s validation is scoped to lesson.PdfDocumentResourceId of the specific lesson
        // in the URL - two lessons sharing the same page-numbering scheme must never let a toggle
        // on one leak an exclusion row (or a job whose TargetId points elsewhere) onto the other.
        var lessonA = await SeedPdfLessonAsync("lesson-a", "doc-a");
        var lessonB = await SeedPdfLessonAsync("lesson-b", "doc-b");

        await _service.ToggleAsync(lessonA.Id, "pdf-page-1", excluded: true);

        Assert.Single(_excludedSlides.Items, x => x.LessonId == lessonA.Id);
        Assert.DoesNotContain(_excludedSlides.Items, x => x.LessonId == lessonB.Id);
        var lessonIndexJob = Assert.Single(_jobs.Items, j => j.JobType == BackgroundJobType.LessonIndex);
        Assert.Equal(lessonA.Id, lessonIndexJob.TargetId);
    }

    [Fact]
    public async Task ToggleAsync_ASlideObjectIdThatOnlyExistsOnAnotherLesson_ThrowsNotFound_NotAnIdorSuccess()
    {
        // P11-03 - the IDOR-shaped case EX-12(ข) exists to close: lessonA's own document has only
        // 1 page, lessonB's has 10 (the shared sample.pdf fixture). "pdf-page-5" is a real,
        // legitimate slideObjectId on lessonB - submitting it against an endpoint scoped to
        // lessonA must be indistinguishable from a page that never existed anywhere, not silently
        // accepted (which would let a caller who only knows lessonA's id delete/restore a vector
        // that actually belongs to lessonB's document).
        var lessonA = await SeedSinglePagePdfLessonAsync("lesson-a", "doc-a-small");
        var lessonB = await SeedPdfLessonAsync("lesson-b", "doc-b-large"); // 10-page sample.pdf

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.ToggleAsync(lessonA.Id, "pdf-page-5", excluded: true));

        Assert.Equal(404, (int)ex.StatusCode);
        Assert.Empty(_excludedSlides.Items);
        Assert.DoesNotContain(_jobs.Items, j => j.TargetId == lessonB.Id);
    }

    // ---- EX-5/EX-6 dual vector tracks --------------------------------------

    [Fact]
    public async Task ToggleAsync_Excluding_EnqueuesBothTheLessonIndexJob_AndTheDocumentPageVectorDeleteJob()
    {
        var lesson = await SeedPdfLessonAsync();
        _documentChunks.Items.Add(new DocumentChunk
        {
            Id = "chunk-1",
            CompanyId = TestFixtures.CompanyId,
            DocumentId = lesson.PdfDocumentResourceId!,
            ChunkKey = "page-1",
            VectorId = $"{lesson.PdfDocumentResourceId}-page-1",
            NamespaceKey = "ns-doc",
            SeqNo = 1,
            Text = "เนื้อหาในเอกสารต้นฉบับ",
            CharCount = 10,
            HasSuspectCharacters = false,
        });

        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: true);

        Assert.Contains(_jobs.Items, j => j.JobType == BackgroundJobType.LessonIndex && j.TargetId == lesson.Id);
        var vectorDeleteJob = Assert.Single(_jobs.Items, j => j.JobType == BackgroundJobType.VectorDelete);
        Assert.Contains(VectorDeleteTargetKind.LessonPage, vectorDeleteJob.PayloadJson);
        Assert.Contains($"{lesson.PdfDocumentResourceId}-page-1", vectorDeleteJob.PayloadJson);
    }

    [Fact]
    public async Task ToggleAsync_Restoring_EmbedsAndUpsertsTheDocumentChunkText_InlineNotAsAJob()
    {
        var lesson = await SeedPdfLessonAsync();
        _documentChunks.Items.Add(new DocumentChunk
        {
            Id = "chunk-1",
            CompanyId = TestFixtures.CompanyId,
            DocumentId = lesson.PdfDocumentResourceId!,
            ChunkKey = "page-1",
            VectorId = $"{lesson.PdfDocumentResourceId}-page-1",
            NamespaceKey = "ns-doc",
            SeqNo = 1,
            Text = "เนื้อหาในเอกสารต้นฉบับ",
            CharCount = 10,
            HasSuspectCharacters = false,
        });
        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: true);
        var jobCountAfterExclude = _jobs.Items.Count;
        var vectorDeleteJobCountAfterExclude = _jobs.Items.Count(j => j.JobType == BackgroundJobType.VectorDelete);

        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: false);

        // No new vector_delete/lesson_page job for the restore direction on track 2 - only the
        // track-1 lesson_index job is enqueued again; the document-copy restore is inline.
        Assert.Equal(jobCountAfterExclude + 1, _jobs.Items.Count);
        Assert.Equal(vectorDeleteJobCountAfterExclude, _jobs.Items.Count(j => j.JobType == BackgroundJobType.VectorDelete));
    }

    // ---- P11-01 (3rd re-check) - legacy duplicate rows must be reconciled by ToggleAsync itself,
    // not merely tolerated by GetOne, so a restore genuinely un-excludes the page ------------------

    [Fact]
    public async Task ToggleAsync_WithTwoLiveLegacyDuplicateRows_RestoringCollapsesThemToOneNonLiveRow()
    {
        // Seeds the exact pre-fix corruption state directly into the repository's backing store -
        // two LIVE rows for the same (LessonId, SlideObjectId) - bypassing ToggleAsync/SaveAsync,
        // which no longer produce this on a clean DB. Before this fix, ToggleAsync only ever
        // resolved/touched whichever single row GetOne happened to pick, leaving the other live
        // duplicate behind - so the page stayed "excluded" even after a "restore" call reported
        // success. ToggleAsync must now reconcile the whole lesson's rows (via
        // LessonExcludedSlideReconciler.ReconcileAndLoad) before acting, the same way
        // ApplyExcludedSlidesAsync (SaveAsync path) already does.
        var lesson = await SeedPdfLessonAsync();
        _excludedSlides.Items.Add(new LessonExcludedSlide
        {
            Id = "exsl-dupe-1",
            CompanyId = TestFixtures.CompanyId,
            LessonId = lesson.Id,
            SlideObjectId = "pdf-page-3",
            CreateDate = DateTime.UtcNow.AddMinutes(-10),
            IsDelete = false,
        });
        _excludedSlides.Items.Add(new LessonExcludedSlide
        {
            Id = "exsl-dupe-2",
            CompanyId = TestFixtures.CompanyId,
            LessonId = lesson.Id,
            SlideObjectId = "pdf-page-3",
            CreateDate = DateTime.UtcNow,
            IsDelete = false,
        });

        await _service.ToggleAsync(lesson.Id, "pdf-page-3", excluded: false);

        // "หน้าละหนึ่งแถว" (EX-4/DM-17) must hold after the call: exactly one row survives for this
        // lesson+slide, and restoring genuinely un-excludes the page - it is not live.
        var remainingRows = _excludedSlides.Items.Where(x => x.LessonId == lesson.Id && x.SlideObjectId == "pdf-page-3").ToList();
        var remainingRow = Assert.Single(remainingRows);
        Assert.True(remainingRow.IsDelete);

        // GetOne resolves deterministically to that same single row afterward.
        var rowAfterToggle = _excludedSlides.GetOne(lesson.Id, "pdf-page-3");
        Assert.NotNull(rowAfterToggle);
        Assert.Equal(remainingRow.Id, rowAfterToggle!.Id);
    }

    [Fact]
    public async Task ToggleAsync_Excluding_ABlankPageWithNoDocumentChunk_StillSucceeds()
    {
        // EX-6 - a blank page never got a DocumentChunk row at index time (PdfTextExtractor skips
        // it) - nothing to remove, and that must not be an error.
        var lesson = await SeedPdfLessonAsync();

        await _service.ToggleAsync(lesson.Id, "pdf-page-1", excluded: true);

        Assert.Single(_excludedSlides.Items, x => !x.IsDelete);
        Assert.DoesNotContain(_jobs.Items, j => j.JobType == BackgroundJobType.VectorDelete);
    }
}

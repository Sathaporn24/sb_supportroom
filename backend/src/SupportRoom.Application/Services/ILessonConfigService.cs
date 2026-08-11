using SupportRoom.Providers.Knowledge;
using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Services;

public sealed class LessonTeachingContentViewModel
{
    public required LessonConfigViewModel Lesson { get; init; }
    public required string EmbedUrl { get; init; }
    public required IReadOnlyList<TeachingSlideViewModel> Slides { get; init; }
}

public interface ILessonConfigService
{
    IReadOnlyList<LessonConfigViewModel> GetAll();
    LessonConfigViewModel GetBySlug(string slug);

    /// <summary>Upsert by slug - mirrors lessons/route.ts's POST (re-resolves presentationId server-side on every save).</summary>
    Task<LessonConfigViewModel> SaveAsync(LessonConfigDto input);
    Task<LessonTeachingContentViewModel> GetTeachingContentBySlugAsync(string slug);

    /// <summary>Preview a PDF already uploaded via /api/documents, before saving the lesson -
    /// mirrors POST /api/slides/resolve + GET /api/slides/content collapsed into one call, since
    /// the file is already stored (no separate "resolve a URL" step needed for an upload).</summary>
    Task<SlidesLessonContent> PreviewPdfAsync(string documentId);

    /// <summary>1-based pageNumber. Rendered on demand from the stored PDF bytes and cached
    /// briefly in memory (a document's bytes never change for a given id) - still resolved live
    /// on a cache miss/expiry, never persisted as a durable copy, same precedent as everything
    /// else here.</summary>
    Task<byte[]> RenderPdfPageAsync(string documentId, int pageNumber);
}

public sealed class LessonConfigService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<ILessonConfigService> logger,
    ISlidesProvider slidesProvider,
    IKnowledgeIndexingService knowledgeIndexingService,
    IDocumentStorageProvider documentStorageProvider,
    IMemoryCache memoryCache)
    : ServiceBase<ILessonConfigService>(unitOfWork, serviceProvider, logger), ILessonConfigService
{
    private readonly ILessonConfigRepository _repository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly IDocumentResourceRepository _documentResourceRepository = unitOfWork.GetRepository<IDocumentResourceRepository>();

    public IReadOnlyList<LessonConfigViewModel> GetAll()
        => _repository.GetAll().ToList().Adapt<List<LessonConfigViewModel>>();

    public LessonConfigViewModel GetBySlug(string slug)
    {
        var entity = _repository.GetBySlug(slug) ?? throw GeneralException.NotFound("บทเรียน");
        return entity.Adapt<LessonConfigViewModel>();
    }

    public async Task<LessonConfigViewModel> SaveAsync(LessonConfigDto input)
    {
        if (!LessonContentSourceType.Allowed.Contains(input.ContentSourceType))
        {
            throw GeneralException.ValidationError($"contentSourceType ต้องเป็น {string.Join(" หรือ ", LessonContentSourceType.Allowed)}");
        }

        var existing = _repository.GetBySlug(input.Slug);

        // Re-resolve presentationId from the source URL server-side so saving never keeps a
        // stale/mismatched id. Sync failures here don't block saving - CS uses the dedicated
        // "Validate/Sync" button (POST /api/slides/resolve) to see the real error. No-ops for a
        // PDF-sourced lesson since SlidesSourceUrl is empty there.
        var presentationId = existing?.PresentationId;
        if (!string.IsNullOrEmpty(input.SlidesSourceUrl))
        {
            try
            {
                var resolved = await slidesProvider.ResolvePresentationAsync(new ResolvePresentationInput
                {
                    SlidesSourceUrl = input.SlidesSourceUrl,
                    SlidesEmbedUrl = input.SlidesEmbedUrl,
                });
                presentationId = resolved.PresentationId;
            }
            catch (Exception ex)
            {
                // Non-blocking: keep the previous presentationId so the save still commits. But log
                // it - a silently-swallowed resolve failure here used to look like "saving randomly
                // doesn't update the slides" with zero trace in the logs.
                Logger.LogWarning(ex, "Slides resolve failed on save for lesson {Slug}; keeping previous presentationId", input.Slug);
            }
        }
        if (input.ContentSourceType == LessonContentSourceType.Pdf)
        {
            // A lesson that switched from Google Slides to PDF shouldn't keep a stale
            // presentationId around - GetTeachingContentBySlugAsync branches on
            // ContentSourceType anyway, but this avoids a dangling, unused reference.
            presentationId = null;
        }

        var now = DateTime.UtcNow;
        var slideConfigs = input.SlideConfigs.Adapt<List<Domain.Entities.SlideConfig>>();

        var isNew = existing is null;
        LessonConfig entity;
        if (existing is null)
        {
            entity = new LessonConfig
            {
                Id = IdGenerator.GenerateId("lesson"),
                CompanyId = CurrentCompanyId,
                Slug = input.Slug,
                Title = input.Title,
                Description = input.Description,
                SlidesSourceUrl = input.SlidesSourceUrl,
                PresentationId = presentationId,
                SlidesEmbedUrl = input.SlidesEmbedUrl,
                ContentSourceType = input.ContentSourceType,
                PdfDocumentResourceId = input.PdfDocumentResourceId,
                IntroWaitMs = input.IntroWaitMs,
                BreathPauseMs = input.BreathPauseMs,
                FinalQuestionWaitMs = input.FinalQuestionWaitMs,
                SlideConfigs = slideConfigs,
                IsActive = input.IsActive,
                CreateDate = now,
                UpdateDate = now,
            };
            _repository.Add(entity);
        }
        else
        {
            // Mutate the already-tracked instance in place - constructing a new object with the
            // same Id and calling Update() on it conflicts with EF Core's change tracker, which
            // is still tracking `existing` from the GetBySlug() lookup above.
            existing.Title = input.Title;
            existing.Description = input.Description;
            existing.SlidesSourceUrl = input.SlidesSourceUrl;
            existing.PresentationId = presentationId;
            existing.SlidesEmbedUrl = input.SlidesEmbedUrl;
            existing.ContentSourceType = input.ContentSourceType;
            existing.PdfDocumentResourceId = input.PdfDocumentResourceId;
            existing.IntroWaitMs = input.IntroWaitMs;
            existing.BreathPauseMs = input.BreathPauseMs;
            existing.FinalQuestionWaitMs = input.FinalQuestionWaitMs;
            existing.SlideConfigs = slideConfigs;
            existing.IsActive = input.IsActive;
            existing.UpdateDate = now;
            _repository.Update(existing);
            entity = existing;
        }
        UnitOfWork.Commit();

        Logger.LogInformation("Lesson {Action}: {Slug} slides={SlideCount}", isNew ? "created" : "updated", input.Slug, slideConfigs.Count);

        // Best-effort re-index for RAG grounding - a Slides API hiccup here must not undo the
        // save that already committed above, it just leaves the knowledge store stale until the
        // next successful save.
        if (!string.IsNullOrEmpty(presentationId))
        {
            try
            {
                var content = await slidesProvider.GetLessonContentAsync(new GetLessonContentInput { PresentationId = presentationId });
                await knowledgeIndexingService.IndexLessonAsync(KnowledgeNamespaces.For(CurrentCompanyId, input.Slug), content.Slides);
            }
            catch (Exception ex)
            {
                // Non-blocking: keep the previously-indexed content. Log it so a broken RAG index
                // (stale answers to voice questions) is diagnosable instead of silent.
                Logger.LogWarning(ex, "RAG re-index failed on save for lesson {Slug}; knowledge store left stale", input.Slug);
            }
        }

        return entity.Adapt<LessonConfigViewModel>();
    }

    public async Task<LessonTeachingContentViewModel> GetTeachingContentBySlugAsync(string slug)
    {
        var lesson = _repository.GetBySlug(slug);
        if (lesson is null || !lesson.IsActive)
        {
            throw GeneralException.NotFound("บทเรียนนี้ หรือยังไม่เปิดใช้งาน");
        }

        var content = lesson.ContentSourceType == LessonContentSourceType.Pdf
            ? await GetPdfContentAsync(lesson)
            : await GetGoogleSlidesContentAsync(lesson);

        var durationBySlide = lesson.SlideConfigs.ToDictionary(s => s.SlideObjectId, s => s.VideoDurationMs ?? 0);
        var slides = content.Slides
            .OrderBy(s => s.Index)
            .Select(s => new TeachingSlideViewModel
            {
                SlideObjectId = s.SlideObjectId,
                Index = s.Index,
                SpeakerNotes = s.SpeakerNotes,
                SlideUrl = s.SlideUrl,
                VideoDurationMs = durationBySlide.GetValueOrDefault(s.SlideObjectId, 0),
            })
            .ToList();

        return new LessonTeachingContentViewModel
        {
            Lesson = lesson.Adapt<LessonConfigViewModel>(),
            EmbedUrl = !string.IsNullOrEmpty(content.EmbedUrl) ? content.EmbedUrl : (lesson.SlidesEmbedUrl ?? ""),
            Slides = slides,
        };
    }

    private async Task<SlidesLessonContent> GetGoogleSlidesContentAsync(LessonConfig lesson)
    {
        if (string.IsNullOrEmpty(lesson.PresentationId))
        {
            throw GeneralException.ConfigError("บทเรียนนี้ยังไม่ได้ตั้งค่า Google Slides");
        }
        try
        {
            return await slidesProvider.GetLessonContentAsync(new GetLessonContentInput { PresentationId = lesson.PresentationId });
        }
        catch (Exception ex)
        {
            throw GeneralException.UpstreamError(ex.Message);
        }
    }

    private async Task<SlidesLessonContent> GetPdfContentAsync(LessonConfig lesson)
    {
        if (string.IsNullOrEmpty(lesson.PdfDocumentResourceId))
        {
            throw GeneralException.ConfigError("บทเรียนนี้ยังไม่ได้อัปโหลดไฟล์ PDF");
        }
        return await BuildPdfContentAsync(lesson.PdfDocumentResourceId);
    }

    public async Task<SlidesLessonContent> PreviewPdfAsync(string documentId) => await BuildPdfContentAsync(documentId);

    /// <summary>
    /// Parsing (ContentOrderTextExtractor + the Thai PUA-glyph fixups + line-joining, per page)
    /// used to re-run in full on every single room-open - cached the same way GetPdfBytesAsync
    /// already caches the raw bytes, since a document's bytes (and therefore its parsed content)
    /// never change for a given id. GetOrCreateAsync only caches a successful result, so a
    /// corrupt/non-PDF file's exception below is never cached - it's re-validated next time too.
    /// </summary>
    private async Task<SlidesLessonContent> BuildPdfContentAsync(string documentId)
    {
        return await memoryCache.GetOrCreateAsync($"pdf-content:{documentId}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            var (document, bytes) = await GetPdfBytesAsync(documentId);
            try
            {
                using var pdfStream = new MemoryStream(bytes, writable: false);
                return PdfSlidesRenderer.BuildContent(pdfStream, document.Id, document.FileName);
            }
            catch (Exception ex) when (ex is not HttpStatusCodeException)
            {
                // A non-PDF upload (the picker also accepts .pptx/.docx/.xlsx) or a corrupt file used to
                // surface as an opaque 500. Give CS a clear, actionable message instead.
                Logger.LogWarning(ex, "PDF content build failed for document {DocumentId} ({FileName})", document.Id, document.FileName);
                throw GeneralException.ValidationError($"ไฟล์ \"{document.FileName}\" อ่านเป็น PDF ไม่ได้ - แหล่งเนื้อหาแบบ PDF ต้องเป็นไฟล์ .pdf เท่านั้น");
            }
        }) ?? throw GeneralException.NotFound("ไฟล์ PDF");
    }

    public async Task<byte[]> RenderPdfPageAsync(string documentId, int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw GeneralException.ValidationError("เลขหน้าต้องเริ่มจาก 1");
        }

        // Page count comes from the cached parsed content instead of a second PDFium parse just
        // to count pages - PdfSlidesRenderer.BuildContent already keeps one ResolvedSlide per PDF
        // page (blank pages included, for continuous numbering), so the two counts are always
        // identical, and after the first call this is a cache hit rather than real work.
        var content = await BuildPdfContentAsync(documentId);
        var pageCount = content.Slides.Count;
        if (pageNumber > pageCount)
        {
            throw GeneralException.NotFound($"หน้า {pageNumber} (เอกสารนี้มี {pageCount} หน้า)");
        }

        return await memoryCache.GetOrCreateAsync($"pdf-page-png:{documentId}:{pageNumber}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            var (_, bytes) = await GetPdfBytesAsync(documentId);
            using var pdfStream = new MemoryStream(bytes, writable: false);
            return PdfSlidesRenderer.RenderPagePng(pdfStream, pageNumber);
        }) ?? throw GeneralException.NotFound("ไฟล์ PDF");
    }

    /// <summary>
    /// Opening a PDF room re-renders every page as an image (one HTTP call per page), and each of
    /// those used to re-download the whole file from storage. A document's bytes never change (a
    /// re-upload gets a fresh id), so cache them briefly - this collapses N storage round-trips
    /// per room-open into one, which matters most once storage is remote (Huawei OBS) rather than
    /// local disk.
    /// </summary>
    private async Task<(DocumentResource Document, byte[] Bytes)> GetPdfBytesAsync(string documentId)
    {
        var document = _documentResourceRepository.Get(documentId)
            ?? throw GeneralException.NotFound("ไฟล์ PDF");
        var bytes = await memoryCache.GetOrCreateAsync($"pdf-bytes:{documentId}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            try
            {
                using var pdfStream = await documentStorageProvider.DownloadAsync(document.ObsKey);
                using var buffer = new MemoryStream();
                await pdfStream.CopyToAsync(buffer);
                return buffer.ToArray();
            }
            catch (Exception ex)
            {
                // The DB row can outlive the physical object (manual deletion, a storage reset
                // without a matching migration, etc.) - used to leak the storage provider's raw
                // exception (local file path / S3 error) as an opaque 500 instead of the same
                // clean 404 a missing DB row already gets.
                Logger.LogWarning(ex, "PDF storage download failed for document {DocumentId} ({FileName})", document.Id, document.FileName);
                throw GeneralException.NotFound("ไฟล์ PDF");
            }
        }) ?? throw GeneralException.NotFound("ไฟล์ PDF");
        return (document, bytes);
    }
}

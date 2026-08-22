using SupportRoom.Providers.Knowledge;
using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
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

public sealed class LearnerLessonConfigViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ContentSourceType { get; init; }
    public required int IntroWaitMs { get; init; }
    public required int BreathPauseMs { get; init; }
    public required int FinalQuestionWaitMs { get; init; }
}

public sealed class LearnerLessonTeachingContentViewModel
{
    public required LearnerLessonConfigViewModel Lesson { get; init; }
    public required string EmbedUrl { get; init; }
    public required IReadOnlyList<TeachingSlideViewModel> Slides { get; init; }
}

public interface ILessonConfigService
{
    IReadOnlyList<LessonConfigViewModel> GetAll();
    LessonConfigViewModel GetBySlug(string slug);

    /// <summary>Upsert by slug - mirrors lessons/route.ts's POST (re-resolves presentationId server-side on every save).</summary>
    Task<LessonConfigViewModel> SaveAsync(LessonConfigDto input);
    Task<LessonConfigViewModel> MoveCategoryAsync(string id, string categoryId);
    Task<LessonTeachingContentViewModel> GetTeachingContentBySlugAsync(string slug);

    /// <summary>Learner-side variant. Adds the link token to any PDF page URLs after the link has
    /// resolved company context, so later anonymous image requests can repeat the same safe lookup.</summary>
    Task<LearnerLessonTeachingContentViewModel> GetTeachingContentByLinkAsync(string token);

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
    IMemoryCache memoryCache,
    ILessonSlideNarrationResolver narrationResolver)
    : ServiceBase<ILessonConfigService>(unitOfWork, serviceProvider, logger), ILessonConfigService
{
    private readonly ILessonConfigRepository _repository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly IDocumentResourceRepository _documentResourceRepository = unitOfWork.GetRepository<IDocumentResourceRepository>();
    private readonly IKnowledgeCategoryRepository _knowledgeCategoryRepository = unitOfWork.GetRepository<IKnowledgeCategoryRepository>();
    private readonly ILessonSlideNarrationRepository _narrationRepository = unitOfWork.GetRepository<ILessonSlideNarrationRepository>();
    private readonly ICompanyRepository _companyRepository = unitOfWork.GetRepository<ICompanyRepository>();

    public IReadOnlyList<LessonConfigViewModel> GetAll()
        => _repository.GetAll().ToList().Adapt<List<LessonConfigViewModel>>();

    public LessonConfigViewModel GetBySlug(string slug)
    {
        var entity = _repository.GetBySlug(slug) ?? throw GeneralException.NotFound("บทเรียน");
        return entity.Adapt<LessonConfigViewModel>();
    }

    public async Task<LessonConfigViewModel> SaveAsync(LessonConfigDto input)
    {
        ValidateSlug(input.Slug);
        ValidateCategory(input.CategoryId);
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
        var previousPdfDocumentResourceId = existing?.PdfDocumentResourceId;
        LessonConfig entity;
        if (existing is null)
        {
            entity = new LessonConfig
            {
                Id = IdGenerator.GenerateId("lesson"),
                CompanyId = CurrentCompanyId,
                Slug = input.Slug,
                CategoryId = input.CategoryId,
                Title = input.Title,
                Description = input.Description,
                SlidesSourceUrl = input.SlidesSourceUrl,
                PresentationId = presentationId,
                SlidesEmbedUrl = input.SlidesEmbedUrl,
                ContentSourceType = input.ContentSourceType,
                PdfDocumentResourceId = input.PdfDocumentResourceId,
                SlideConfigs = slideConfigs,
                IsActive = input.IsActive,
                CreateBy = CurrentUserId,
                CreateDate = now,
                UpdateBy = CurrentUserId,
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
            existing.CategoryId = input.CategoryId;
            existing.Description = input.Description;
            existing.SlidesSourceUrl = input.SlidesSourceUrl;
            existing.PresentationId = presentationId;
            existing.SlidesEmbedUrl = input.SlidesEmbedUrl;
            existing.ContentSourceType = input.ContentSourceType;
            existing.PdfDocumentResourceId = input.PdfDocumentResourceId;
            existing.SlideConfigs = slideConfigs;
            existing.IsActive = input.IsActive;
            existing.UpdateBy = CurrentUserId;
            existing.UpdateDate = now;
            _repository.Update(existing);
            entity = existing;
        }

        // NR-3 - a PDF re-upload (or switching away from PDF) invalidates every CS-authored
        // narration override in one shot: pdf-page-N is a raw page index, so a different file
        // silently shifts every later page onto the wrong narration with no error (NR-4 - no
        // heuristic page-matching is attempted). Soft-deleted in the same transaction as this
        // save, not a separate request, so there is no window where the two are inconsistent.
        if (!isNew && previousPdfDocumentResourceId != input.PdfDocumentResourceId)
        {
            _narrationRepository.DeleteByLessonId(entity.Id);
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
        else if (input.ContentSourceType == LessonContentSourceType.Pdf && !string.IsNullOrEmpty(entity.PdfDocumentResourceId))
        {
            // NR-7 - a PDF lesson was never indexed into its own namespace at all before this
            // (only the `if (!string.IsNullOrEmpty(presentationId))` branch above ever ran).
            // Narration overrides go through the same NR-1 resolver as the tutor-facing content
            // path, so what gets indexed here always matches what CS just heard/read in preview.
            try
            {
                var pdfContent = await BuildPdfContentAsync(entity.PdfDocumentResourceId);
                var resolvedSlides = await narrationResolver.ResolveAsync(entity.Id, pdfContent.Slides);
                await knowledgeIndexingService.IndexLessonAsync(KnowledgeNamespaces.For(CurrentCompanyId, input.Slug), resolvedSlides);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "RAG re-index failed on save for PDF lesson {Slug}; knowledge store left stale", input.Slug);
            }
        }

        return entity.Adapt<LessonConfigViewModel>();
    }

    public Task<LessonConfigViewModel> MoveCategoryAsync(string id, string categoryId)
    {
        ValidateCategory(categoryId);
        var lesson = _repository.Get(id) ?? throw GeneralException.NotFound("บทเรียน");
        lesson.CategoryId = categoryId;
        lesson.UpdateBy = CurrentUserId;
        lesson.UpdateDate = DateTime.UtcNow;
        _repository.Update(lesson);
        UnitOfWork.Commit();
        return Task.FromResult(lesson.Adapt<LessonConfigViewModel>());
    }

    private static void ValidateSlug(string slug)
    {
        if (slug.StartsWith("kbcat-", StringComparison.OrdinalIgnoreCase)
            || string.Equals(slug, "kb-global", StringComparison.OrdinalIgnoreCase))
        {
            throw GeneralException.ValidationError("Slug ห้ามขึ้นต้นด้วย kbcat- หรือเท่ากับ kb-global");
        }
    }

    private void ValidateCategory(string categoryId)
    {
        var category = _knowledgeCategoryRepository.Get(categoryId) ?? throw GeneralException.ValidationError("ไม่พบหมวดที่เลือก");
        if (category.Level != 2)
        {
            throw GeneralException.ValidationError("บทเรียนต้องอยู่ในหมวดย่อยเท่านั้น");
        }
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

    public async Task<LearnerLessonTeachingContentViewModel> GetTeachingContentByLinkAsync(string token)
    {
        var link = ServiceProvider.GetRequiredService<ITrainingLinkService>().GetEntityByToken(token);
        var content = await GetTeachingContentBySlugAsync(link.LessonSlug);

        // LP-1/LP-4 - pacing is a company-level default with no per-lesson override anymore
        // (N1/N2/N3, 2026-08-22) - read straight off Company.Default*Ms. This is the one place in
        // the system this is read; ICompanyService.Create/SeedFirstCompanyIfEmpty are the only
        // places it is written (LP-2).
        var company = _companyRepository.Get(link.CompanyId) ?? throw GeneralException.NotFound("บริษัท");

        var slides = content.Slides.Select(slide => new TeachingSlideViewModel
        {
            SlideObjectId = slide.SlideObjectId,
            Index = slide.Index,
            SpeakerNotes = slide.SpeakerNotes,
            SlideUrl = ToPublicPdfPageUrl(slide.SlideUrl, token),
            VideoDurationMs = slide.VideoDurationMs,
        }).ToList();

        return new LearnerLessonTeachingContentViewModel
        {
            Lesson = new LearnerLessonConfigViewModel
            {
                Slug = content.Lesson.Slug,
                Title = content.Lesson.Title,
                Description = content.Lesson.Description,
                ContentSourceType = content.Lesson.ContentSourceType,
                IntroWaitMs = company.DefaultIntroWaitMs,
                BreathPauseMs = company.DefaultBreathPauseMs,
                FinalQuestionWaitMs = company.DefaultFinalQuestionWaitMs,
            },
            EmbedUrl = content.EmbedUrl,
            Slides = slides,
        };
    }

    private static string? ToPublicPdfPageUrl(string? slideUrl, string token)
    {
        if (string.IsNullOrEmpty(slideUrl) || !slideUrl.StartsWith("pdf-page:", StringComparison.Ordinal))
        {
            return slideUrl;
        }

        var parts = slideUrl.Split(':', 3);
        return parts.Length == 3
            ? $"/api/lessons/pdf-pages/{Uri.EscapeDataString(token)}/{Uri.EscapeDataString(parts[1])}/{parts[2]}"
            : null;
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
        var content = await BuildPdfContentAsync(lesson.PdfDocumentResourceId);

        // NR-1 - apply CS-authored narration overrides on top of the extracted text. This is the
        // tutor-facing consumer of the shared resolver; ProcessLessonIndexAsync (Application
        // layer, BackgroundJobProcessor) is the other, so what the tutor engine speaks and what
        // the RAG index answers from can never disagree.
        var resolvedSlides = await narrationResolver.ResolveAsync(lesson.Id, content.Slides);
        return ReferenceEquals(resolvedSlides, content.Slides)
            ? content
            : new SlidesLessonContent
            {
                PresentationId = content.PresentationId,
                Title = content.Title,
                EmbedUrl = content.EmbedUrl,
                Slides = resolvedSlides,
                SyncedAt = content.SyncedAt,
            };
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

using System.Text.Json;
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
using SupportRoom.Providers.Storage;

namespace SupportRoom.Application.Tests;

public class DocumentResourceServiceTests
{
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeKnowledgeCategoryRepository _categories = new();
    private readonly FakeBackgroundJobRepository _jobs = new();
    private readonly FakeDocumentChunkRepository _chunks = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DocumentResourceService _service;

    public DocumentResourceServiceTests()
    {
        MapsterConfig.Apply();
        _unitOfWork
            .Register<IDocumentResourceRepository>(_documents)
            .Register<ILessonConfigRepository>(_lessons)
            .Register<IKnowledgeCategoryRepository>(_categories)
            .Register<IBackgroundJobRepository>(_jobs)
            .Register<IDocumentChunkRepository>(_chunks);
        _service = new DocumentResourceService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IDocumentResourceService>.Instance,
            new LocalDocumentStorageProvider(NullLogger<LocalDocumentStorageProvider>.Instance),
            TestFixtures.GuardFor(AdminRole.Admin, TestFixtures.CompanyId),
            new KnowledgeNamespaceResolver(_unitOfWork));
    }

    private KnowledgeCategory SeedCategory(string id, int level, string? parentId = null, bool isSystemDefault = false)
    {
        var category = new KnowledgeCategory
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            ParentId = parentId,
            Level = level,
            Name = $"category-{id}",
            SortOrder = 0,
            IsSystemDefault = isSystemDefault,
        };
        _categories.Items.Add(category);
        return category;
    }

    private DocumentResource SeedDocument(string id = "doc-1", string? lessonId = null)
    {
        var doc = new DocumentResource
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            ScopeType = lessonId is null ? "company" : "lesson",
            ScopeId = lessonId,
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

    private DocumentChunk SeedChunk(
        string documentId, string chunkKey, int seqNo, string namespaceKey = "company-test:kb-global", bool hasSuspectCharacters = false)
    {
        var chunk = new DocumentChunk
        {
            Id = $"chunk-{documentId}-{chunkKey}",
            CompanyId = TestFixtures.CompanyId,
            CreateDate = DateTime.UtcNow,
            DocumentId = documentId,
            ChunkKey = chunkKey,
            VectorId = $"{documentId}-{chunkKey}",
            NamespaceKey = namespaceKey,
            SeqNo = seqNo,
            Text = $"text {seqNo}",
            CharCount = 7,
            HasSuspectCharacters = hasSuspectCharacters,
        };
        _chunks.Items.Add(chunk);
        return chunk;
    }

    private void SeedLessonUsingPdf(string slug, string pdfDocumentResourceId)
        => _lessons.Items.Add(new LessonConfig
        {
            Id = $"lesson-{slug}",
            CompanyId = TestFixtures.CompanyId,
            Slug = slug,
            CategoryId = "kbcat-child",
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
    public async Task DeleteAsync_SoftDeletesTheDocument_AndQueuesVectorCleanup_WhenNotReferenced()
    {
        SeedDocument("doc-1");
        SeedChunk("doc-1", "page-1", 1);
        SeedChunk("doc-1", "page-2", 2);

        await _service.DeleteAsync("doc-1");

        // Soft delete (DI-13), not removed: the row must stay reachable via GetDeleted() for
        // restore, and the file it points at is left in storage.
        var deleted = Assert.Single(_documents.Items);
        Assert.True(deleted.IsDelete);
        Assert.NotNull(deleted.DeletedAt);

        var job = Assert.Single(_jobs.Items);
        Assert.Equal("vector_delete", job.JobType);
        Assert.Equal("doc-1", job.TargetId);
        Assert.Equal(1, _unitOfWork.CommitCount);

        // DI-13 - PayloadJson carries the real VectorIds read from DocumentChunk, not
        // recomputed by re-extracting the file.
        var payload = JsonSerializer.Deserialize<VectorDeleteJobPayload>(job.PayloadJson!)!;
        Assert.Equal("company-test:kb-global", payload.NamespaceKey);
        Assert.Equal(["doc-1-page-1", "doc-1-page-2"], payload.VectorIds);

        // DI-8's replace-the-whole-set is undone by delete: the chunk rows themselves go away too.
        Assert.All(_chunks.Items.Where(c => c.DocumentId == "doc-1"), c => Assert.True(c.IsDelete));
    }

    [Fact]
    public async Task DeleteAsync_DoesNotQueueVectorCleanup_WhenDocumentHasNoPersistedChunks()
    {
        SeedDocument("doc-1"); // never successfully indexed - no DocumentChunk rows

        await _service.DeleteAsync("doc-1");

        Assert.Empty(_jobs.Items);
    }

    [Fact]
    public void GetDeleted_ReturnsOnlySoftDeletedDocuments()
    {
        var deleted = SeedDocument("doc-deleted");
        deleted.IsDelete = true;
        deleted.DeletedAt = DateTime.UtcNow;
        SeedDocument("doc-active");

        var list = _service.GetDeleted();

        Assert.Single(list);
        Assert.Equal("doc-deleted", list[0].Id);
    }

    [Fact]
    public async Task RestoreAsync_ThrowsNotFound_WhenDocumentIsNotSoftDeleted()
    {
        SeedDocument("doc-active");

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.RestoreAsync("doc-active"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task RestoreAsync_ClearsSoftDelete_AndQueuesReindex()
    {
        var deleted = SeedDocument("doc-1");
        deleted.IsDelete = true;
        deleted.DeletedAt = DateTime.UtcNow;
        deleted.IndexingStatus = "failed";
        deleted.FailureReason = "extract_failed";

        await _service.RestoreAsync("doc-1");

        Assert.False(deleted.IsDelete);
        Assert.Null(deleted.DeletedAt);
        Assert.Equal("pending", deleted.IndexingStatus);
        Assert.Null(deleted.FailureReason);

        var job = Assert.Single(_jobs.Items);
        Assert.Equal("document_index", job.JobType);
        Assert.Equal("doc-1", job.TargetId);
    }

    [Fact]
    public void GetByScope_DefaultsToCompany_WhenNoQuerySent()
    {
        // DS-4 - omitting scopeType/scopeId entirely must keep the old central-library-screen
        // behaviour: company scope, not an error.
        SeedDocument("doc-standalone", lessonId: null);
        SeedDocument("doc-attached", lessonId: "lesson-x");

        var list = _service.GetByScope(null, null);

        Assert.Single(list);
        Assert.Equal("doc-standalone", list[0].Id);
    }

    [Fact]
    public void GetByScope_ReturnsDocumentsOfTheRequestedLesson()
    {
        SeedDocument("doc-standalone", lessonId: null);
        SeedDocument("doc-attached", lessonId: "lesson-x");

        var list = _service.GetByScope("lesson", "lesson-x");

        Assert.Single(list);
        Assert.Equal("doc-attached", list[0].Id);
    }

    [Fact]
    public void GetChunks_ThrowsNotFound_WhenDocumentMissing()
    {
        var ex = Assert.Throws<HttpStatusCodeException>(() => _service.GetChunks("ghost"));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public void GetChunks_ReturnsRowsOrderedBySeqNo_WithSuspectFlagIntact()
    {
        SeedDocument("doc-1");
        SeedChunk("doc-1", "page-2", seqNo: 2);
        SeedChunk("doc-1", "page-1", seqNo: 1, hasSuspectCharacters: true);

        var list = _service.GetChunks("doc-1");

        Assert.Equal(2, list.Count);
        Assert.Equal("page-1", list[0].ChunkKey);
        Assert.True(list[0].HasSuspectCharacters);
        Assert.Equal("page-2", list[1].ChunkKey);
        Assert.False(list[1].HasSuspectCharacters);
    }

    [Fact]
    public void GetChunks_ThrowsUnauthorized_WhenNobodyIsSignedIn()
    {
        SeedDocument("doc-1");
        var anonymousService = new DocumentResourceService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IDocumentResourceService>.Instance,
            new LocalDocumentStorageProvider(NullLogger<LocalDocumentStorageProvider>.Instance),
            TestFixtures.AnonymousGuard(),
            new KnowledgeNamespaceResolver(_unitOfWork));

        var ex = Assert.Throws<HttpStatusCodeException>(() => anonymousService.GetChunks("doc-1"));
        Assert.Equal(401, (int)ex.StatusCode);
    }

    private static UploadDocumentDto UploadDto(string scopeType, string? scopeId)
        => new()
        {
            Content = [1, 2, 3],
            FileName = "manual.pdf",
            ContentType = "application/pdf",
            ScopeType = scopeType,
            ScopeId = scopeId,
        };

    // DS-12 / DS-2 - UploadAsync goes through the real KnowledgeNamespaceResolver.EnsureValidScope,
    // not a mock, so a "category" scope actually reaches the resolver's Level-2 check.
    [Fact]
    public async Task UploadAsync_Succeeds_WhenScopeIsAValidLevel2Category()
    {
        SeedCategory("kbcat-parent", level: 1);
        SeedCategory("kbcat-child", level: 2, parentId: "kbcat-parent");

        var result = await _service.UploadAsync(UploadDto(KnowledgeScopeType.Category, "kbcat-child"));

        Assert.Equal(KnowledgeScopeType.Category, result.ScopeType);
        Assert.Equal("kbcat-child", result.ScopeId);
        Assert.Single(_documents.Items);
    }

    // DS-3 - all 6 rejection cases, all surfaced through EnsureValidScope inside UploadAsync.
    [Fact]
    public async Task UploadAsync_Rejects_UnknownScopeType()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UploadAsync(UploadDto("not-a-scope", null)));
        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_documents.Items); // DS-2 - rejected before anything is written
    }

    [Fact]
    public async Task UploadAsync_Rejects_LessonScopeId_NotInThisCompany()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UploadAsync(UploadDto(KnowledgeScopeType.Lesson, "ghost-lesson")));
        Assert.Equal(404, (int)ex.StatusCode);
        Assert.Empty(_documents.Items);
    }

    [Fact]
    public async Task UploadAsync_Rejects_CategoryThatIsALevel1Parent()
    {
        SeedCategory("kbcat-parent", level: 1);

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UploadAsync(UploadDto(KnowledgeScopeType.Category, "kbcat-parent")));
        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_documents.Items);
    }

    [Fact]
    public async Task UploadAsync_Rejects_CategoryScopeId_ThatDoesNotExist()
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UploadAsync(UploadDto(KnowledgeScopeType.Category, "ghost-category")));
        Assert.Equal(404, (int)ex.StatusCode);
        Assert.Empty(_documents.Items);
    }

    [Fact]
    public async Task UploadAsync_Rejects_CompanyScope_WithAScopeId()
    {
        // KS-2 - "company" must reject a stray ScopeId outright, not silently ignore it.
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UploadAsync(UploadDto(KnowledgeScopeType.Company, "some-id")));
        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_documents.Items);
    }

    [Theory]
    [InlineData(KnowledgeScopeType.Lesson)]
    [InlineData(KnowledgeScopeType.Category)]
    public async Task UploadAsync_Rejects_LessonOrCategoryScope_WithNoScopeId(string scopeType)
    {
        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UploadAsync(UploadDto(scopeType, null)));
        Assert.Equal(400, (int)ex.StatusCode);
        Assert.Empty(_documents.Items);
    }

    // DS-6/DS-12 - moving scope groups DocumentChunk by NamespaceKey, creates one vector_delete
    // job per group with the exact ids that were upserted, then resets indexing state to pending.
    [Fact]
    public async Task MoveScopeAsync_QueuesVectorDelete_AndReindex_WhenScopeActuallyChanges()
    {
        SeedDocument("doc-1", lessonId: null); // starts at company scope
        SeedChunk("doc-1", "page-1", 1, namespaceKey: "company-test:kb-global");
        SeedChunk("doc-1", "page-2", 2, namespaceKey: "company-test:kb-global");
        SeedCategory("kbcat-parent", level: 1);
        SeedCategory("kbcat-child", level: 2, parentId: "kbcat-parent");

        var result = await _service.MoveScopeAsync("doc-1", new MoveDocumentScopeDto { ScopeType = KnowledgeScopeType.Category, ScopeId = "kbcat-child" });

        Assert.Equal(KnowledgeScopeType.Category, result.ScopeType);
        Assert.Equal("kbcat-child", result.ScopeId);
        Assert.Equal("pending", result.IndexingStatus);
        Assert.Equal(0, result.IndexedChunkCount);

        var stored = _documents.Items.Single(d => d.Id == "doc-1");
        Assert.Equal("pending", stored.IndexingStatus);
        Assert.Equal(0, stored.IndexedChunkCount);
        Assert.Null(stored.FailureReason);

        var vectorDeleteJob = Assert.Single(_jobs.Items, j => j.JobType == "vector_delete");
        var payload = JsonSerializer.Deserialize<VectorDeleteJobPayload>(vectorDeleteJob.PayloadJson!)!;
        Assert.Equal("company-test:kb-global", payload.NamespaceKey);
        Assert.Equal(["doc-1-page-1", "doc-1-page-2"], payload.VectorIds);

        var reindexJob = Assert.Single(_jobs.Items, j => j.JobType == "document_index");
        Assert.Equal("doc-1", reindexJob.TargetId);

        Assert.All(_chunks.Items.Where(c => c.DocumentId == "doc-1"), c => Assert.True(c.IsDelete));
    }

    [Fact]
    public async Task MoveScopeAsync_IsANoOp_WhenMovingToTheExactSameScope()
    {
        var doc = SeedDocument("doc-1", lessonId: null); // company scope
        SeedChunk("doc-1", "page-1", 1);

        var result = await _service.MoveScopeAsync("doc-1", new MoveDocumentScopeDto { ScopeType = doc.ScopeType, ScopeId = doc.ScopeId });

        Assert.Equal("indexed", result.IndexingStatus); // unchanged - SeedDocument defaults to "indexed"
        Assert.Empty(_jobs.Items);
        Assert.False(_chunks.Items.Single().IsDelete);
    }

    [Fact]
    public async Task MoveScopeAsync_DoesNotQueueVectorDelete_WhenDocumentHasNoPersistedChunks()
    {
        SeedDocument("doc-1", lessonId: null); // never successfully indexed - no DocumentChunk rows
        SeedCategory("kbcat-parent", level: 1);
        SeedCategory("kbcat-child", level: 2, parentId: "kbcat-parent");

        await _service.MoveScopeAsync("doc-1", new MoveDocumentScopeDto { ScopeType = KnowledgeScopeType.Category, ScopeId = "kbcat-child" });

        Assert.DoesNotContain(_jobs.Items, j => j.JobType == "vector_delete");
        var reindexJob = Assert.Single(_jobs.Items, j => j.JobType == "document_index");
        Assert.Equal("doc-1", reindexJob.TargetId);
    }

    [Fact]
    public async Task MoveScopeAsync_ThrowsNotFound_WhenDocumentIsSoftDeleted()
    {
        var deleted = SeedDocument("doc-1", lessonId: null);
        deleted.IsDelete = true;
        deleted.DeletedAt = DateTime.UtcNow;

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.MoveScopeAsync("doc-1", new MoveDocumentScopeDto { ScopeType = KnowledgeScopeType.Company, ScopeId = null }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task MoveScopeAsync_Rejects_InvalidTargetScope()
    {
        SeedDocument("doc-1", lessonId: null);

        var ex = await Assert.ThrowsAsync<HttpStatusCodeException>(
            () => _service.MoveScopeAsync("doc-1", new MoveDocumentScopeDto { ScopeType = KnowledgeScopeType.Lesson, ScopeId = "ghost-lesson" }));
        Assert.Equal(404, (int)ex.StatusCode);
    }

    [Fact]
    public async Task MoveScopeAsync_Allowed_ForADocumentThatIsALessonsPdfSource()
    {
        // DS-7 - moving is not the same operation as DeleteAsync's block: a document a lesson
        // uses as its PDF source can still be moved, unlike DeleteAsync which refuses.
        SeedDocument("doc-1", lessonId: null);
        SeedLessonUsingPdf("login-pdf", "doc-1");

        var result = await _service.MoveScopeAsync("doc-1", new MoveDocumentScopeDto { ScopeType = KnowledgeScopeType.Lesson, ScopeId = "lesson-login-pdf" });

        Assert.Equal(KnowledgeScopeType.Lesson, result.ScopeType);
        Assert.Equal("lesson-login-pdf", result.ScopeId);
    }
}

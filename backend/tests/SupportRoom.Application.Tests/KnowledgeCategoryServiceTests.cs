using Microsoft.Extensions.Logging.Abstractions;
using SupportRoom.Application.Common;
using SupportRoom.Application.Dto;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Entities;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

public class KnowledgeCategoryServiceTests
{
    private readonly FakeKnowledgeCategoryRepository _categories = new();
    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeDocumentResourceRepository _documents = new();
    private readonly FakeKnowledgeQnARepository _qnas = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly KnowledgeCategoryService _service;

    public KnowledgeCategoryServiceTests()
    {
        _unitOfWork
            .Register<IKnowledgeCategoryRepository>(_categories)
            .Register<ILessonConfigRepository>(_lessons)
            .Register<IDocumentResourceRepository>(_documents)
            .Register<IKnowledgeQnARepository>(_qnas);
        _service = new KnowledgeCategoryService(
            _unitOfWork,
            new FakeServiceProvider(),
            NullLogger<IKnowledgeCategoryService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_DerivesLevelFromParentId()
    {
        var parent = SeedCategory("parent", null, 1);

        var result = await _service.CreateAsync(new CreateKnowledgeCategoryDto
        {
            ParentId = parent.Id,
            Name = " บัญชี ",
            SortOrder = 2,
        });

        Assert.Equal(2, result.Level);
        Assert.Equal("บัญชี", result.Name);
    }

    [Fact]
    public async Task CreateAsync_RejectsAThirdLevel()
    {
        var child = SeedCategory("child", "parent", 2);

        var error = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.CreateAsync(new CreateKnowledgeCategoryDto
        {
            ParentId = child.Id,
            Name = "ชั้นที่สาม",
            SortOrder = 0,
        }));

        Assert.Equal(400, (int)error.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ReportsDependentCountsAndKeepsCategory()
    {
        var category = SeedCategory("category", null, 1);
        _lessons.Items.Add(new LessonConfig
        {
            Id = "lesson-1",
            CompanyId = TestFixtures.CompanyId,
            Slug = "lesson-a",
            CategoryId = category.Id,
            Title = "บทเรียน",
            SlidesSourceUrl = "",
            ContentSourceType = "google_slides",
            IntroWaitMs = 0,
            BreathPauseMs = 0,
            FinalQuestionWaitMs = 0,
            SlideConfigs = [],
            IsActive = true,
        });
        _documents.Items.Add(new DocumentResource
        {
            Id = "doc-1",
            CompanyId = TestFixtures.CompanyId,
            ScopeType = "category",
            ScopeId = category.Id,
            FileName = "manual.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1,
            ObsBucket = "bucket",
            ObsKey = "key",
            IndexingStatus = "indexed",
        });

        var error = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.DeleteAsync(category.Id));

        Assert.Contains("บทเรียน 1", error.Message);
        Assert.Contains("เอกสาร 1", error.Message);
        Assert.Contains("Q&A 0", error.Message);
        Assert.False(category.IsDelete);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task UpdateAsync_OnEitherRowOfTheDefaultChain_IsBlocked(int level)
    {
        // The backfill migration flags BOTH rows of the chain (Level-1 parent, Level-2 leaf) as
        // IsSystemDefault - a customer must not be able to rename or re-describe either half of
        // "ยังไม่จัดหมวด" out from under the data that still points at it.
        var category = SeedCategory($"default-{level}", level == 2 ? "default-1" : null, level, isSystemDefault: true);

        var error = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.UpdateAsync(category.Id, new UpdateKnowledgeCategoryDto
        {
            Name = "ชื่อใหม่",
            SortOrder = 0,
        }));

        Assert.Equal(400, (int)error.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task DeleteAsync_OnEitherRowOfTheDefaultChain_IsBlocked(int level)
    {
        var category = SeedCategory($"default-{level}", level == 2 ? "default-1" : null, level, isSystemDefault: true);

        var error = await Assert.ThrowsAsync<HttpStatusCodeException>(() => _service.DeleteAsync(category.Id));

        Assert.Equal(400, (int)error.StatusCode);
        Assert.False(category.IsDelete);
    }

    [Fact]
    public void GetSystemDefault_WithTheFlaggedParentAndLeafChain_ReturnsTheLeaf()
    {
        // The chain the backfill migration writes: a Level-1 parent and its Level-2 child both
        // carry IsSystemDefault - LessonConfig.CategoryId always points at the leaf, so callers
        // that resolve "the fallback category" must get the leaf back, not the parent.
        SeedCategory("default-1", null, 1, isSystemDefault: true);
        var leaf = SeedCategory("default-2", "default-1", 2, isSystemDefault: true);

        var result = _categories.GetSystemDefault();

        Assert.Equal(leaf.Id, result?.Id);
    }

    [Fact]
    public void GetSystemDefault_WithTwoFlaggedLeaves_FailsFastInsteadOfPickingOne()
    {
        // Two Level-2 rows both flagged IsSystemDefault is a data-integrity violation the backfill
        // is not supposed to produce. SingleOrDefault throwing here is the intended behavior - a
        // silent pick-one would let corrupted seed data resolve to a category quietly instead of
        // surfacing the bug.
        SeedCategory("default-1", null, 1, isSystemDefault: true);
        SeedCategory("default-2a", "default-1", 2, isSystemDefault: true);
        SeedCategory("default-2b", "default-1", 2, isSystemDefault: true);

        Assert.Throws<InvalidOperationException>(() => _categories.GetSystemDefault());
    }

    private KnowledgeCategory SeedCategory(string id, string? parentId, int level, bool isSystemDefault = false)
    {
        var category = new KnowledgeCategory
        {
            Id = id,
            CompanyId = TestFixtures.CompanyId,
            ParentId = parentId,
            Level = level,
            Name = id,
            SortOrder = 0,
            IsSystemDefault = isSystemDefault,
        };
        _categories.Items.Add(category);
        return category;
    }
}

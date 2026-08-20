using SupportRoom.Application.Exceptions;
using SupportRoom.Application.Services;
using SupportRoom.Application.Tests.Fakes;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Tests;

/// <summary>
/// KS-1 - proves the single resolver that converts (ScopeType, ScopeId) into a namespace key for
/// every scope kind, so DocumentResource and KnowledgeQnA (Phase 3/6) never have to assemble a
/// namespace key themselves. Also covers KS-2's ScopeId rules (lesson/category must exist in this
/// company, company must have a null ScopeId).
/// </summary>
public class KnowledgeNamespaceResolverTests
{
    private const string CompanyId = "company-1";

    private readonly FakeLessonConfigRepository _lessons = new();
    private readonly FakeKnowledgeCategoryRepository _categories = new();
    private readonly KnowledgeNamespaceResolver _resolver;

    public KnowledgeNamespaceResolverTests()
    {
        var unitOfWork = new FakeUnitOfWork()
            .Register<ILessonConfigRepository>(_lessons)
            .Register<IKnowledgeCategoryRepository>(_categories);
        _resolver = new KnowledgeNamespaceResolver(unitOfWork);
    }

    [Fact]
    public void Resolve_LessonScope_UsesTheLessonsSlugNotItsId()
    {
        _lessons.Items.Add(new LessonConfig
        {
            Id = "lesson-1",
            CompanyId = CompanyId,
            CategoryId = "kbcat-child",
            Slug = "how-to-login",
            Title = "เข้าสู่ระบบ",
            SlidesSourceUrl = "",
            ContentSourceType = LessonContentSourceType.GoogleSlides,
            IntroWaitMs = 0,
            BreathPauseMs = 0,
            FinalQuestionWaitMs = 0,
            SlideConfigs = [],
            IsActive = true,
        });

        var result = _resolver.Resolve(CompanyId, KnowledgeScopeType.Lesson, "lesson-1");

        Assert.Equal($"{CompanyId}:how-to-login", result);
    }

    [Fact]
    public void Resolve_LessonScope_ScopeIdNotFound_Throws()
    {
        var error = Assert.Throws<HttpStatusCodeException>(() => _resolver.Resolve(CompanyId, KnowledgeScopeType.Lesson, "missing-lesson"));

        Assert.Equal(404, (int)error.StatusCode);
    }

    [Fact]
    public void Resolve_CategoryScope_UsesTheCategoryIdDirectly()
    {
        var result = _resolver.Resolve(CompanyId, KnowledgeScopeType.Category, "kbcat-accounting");

        Assert.Equal($"{CompanyId}:kbcat-accounting", result);
    }

    [Fact]
    public void Resolve_CompanyScope_IgnoresScopeIdAndReturnsTheGlobalNamespace()
    {
        var result = _resolver.Resolve(CompanyId, KnowledgeScopeType.Company, null);

        Assert.Equal($"{CompanyId}:kb-global", result);
    }

    [Fact]
    public void Resolve_CompanyScope_WithAScopeId_IsRejectedNotIgnored()
    {
        // KS-2: a ScopeId sent alongside ScopeType=company must be rejected outright, not
        // silently dropped - silently ignoring it would let a caller believe something was scoped
        // to a category/lesson when it was actually saved company-wide.
        var error = Assert.Throws<HttpStatusCodeException>(() => _resolver.Resolve(CompanyId, KnowledgeScopeType.Company, "some-id"));

        Assert.Equal(400, (int)error.StatusCode);
    }

    [Fact]
    public void EnsureValidScope_CategoryScope_RejectsALevel1Parent()
    {
        _categories.Items.Add(new KnowledgeCategory
        {
            Id = "kbcat-parent",
            CompanyId = CompanyId,
            ParentId = null,
            Name = "แม่",
            Level = 1,
            SortOrder = 0,
            IsSystemDefault = false,
        });

        var error = Assert.Throws<HttpStatusCodeException>(
            () => _resolver.EnsureValidScope(CompanyId, KnowledgeScopeType.Category, "kbcat-parent"));

        Assert.Equal(400, (int)error.StatusCode);
    }

    [Fact]
    public void EnsureValidScope_LessonScope_MissingScopeId_Throws()
    {
        var error = Assert.Throws<HttpStatusCodeException>(
            () => _resolver.EnsureValidScope(CompanyId, KnowledgeScopeType.Lesson, null));

        Assert.Equal(400, (int)error.StatusCode);
    }
}

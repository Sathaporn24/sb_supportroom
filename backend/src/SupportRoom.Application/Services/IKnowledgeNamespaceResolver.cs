using SupportRoom.Application.Exceptions;
using SupportRoom.Domain.Entities;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;
using SupportRoom.Providers.Knowledge;

namespace SupportRoom.Application.Services;

/// <summary>
/// KS-1 - the single function that turns a (ScopeType, ScopeId) pair into a vector-store namespace
/// key, for both DocumentResource and KnowledgeQnA. Vectors live outside PostgreSQL, so the
/// database's company query filter cannot reach them - the namespace key is the only isolation the
/// knowledge store has. No other call site may assemble a namespace key from a ScopeType/ScopeId
/// pair; go through this resolver instead, so there is exactly one place that can get it wrong.
/// </summary>
public interface IKnowledgeNamespaceResolver
{
    /// <summary>Resolves the namespace for an already-valid scope pair. Throws if scopeId doesn't
    /// resolve to a real row in this company (lesson/category) or is non-null where it must be
    /// null (company) - callers that accept scope input from a request should run
    /// <see cref="EnsureValidScope"/> first so that failure surfaces as a validation error, not an
    /// unhandled exception.</summary>
    string Resolve(string companyId, string scopeType, string? scopeId);

    /// <summary>KS-2 - validates a (ScopeType, ScopeId) pair before every save: "company" must have
    /// a null ScopeId (sent = rejected, not silently ignored), "lesson"/"category" must point at a
    /// real row that belongs to this company. Throws GeneralException.ValidationError /
    /// GeneralException.NotFound on failure - never silently accepts a malformed pair.</summary>
    void EnsureValidScope(string companyId, string scopeType, string? scopeId);
}

public sealed class KnowledgeNamespaceResolver(IUnitOfWork unitOfWork) : IKnowledgeNamespaceResolver
{
    // Repositories are never injected directly in this codebase - every service resolves them
    // from IUnitOfWork.GetRepository<T>() in its constructor body (see ServiceBase-derived
    // services throughout). Nothing registers ILessonConfigRepository/IKnowledgeCategoryRepository
    // as a standalone DI service, so constructor-injecting them here made the app fail to start.
    private readonly ILessonConfigRepository lessonConfigRepository = unitOfWork.GetRepository<ILessonConfigRepository>();
    private readonly IKnowledgeCategoryRepository knowledgeCategoryRepository = unitOfWork.GetRepository<IKnowledgeCategoryRepository>();

    public string Resolve(string companyId, string scopeType, string? scopeId)
        => scopeType switch
        {
            KnowledgeScopeType.Lesson => KnowledgeNamespaces.For(companyId, GetLessonOrThrow(scopeId).Slug),
            KnowledgeScopeType.Category => KnowledgeNamespaces.ForCategory(companyId, RequireScopeId(scopeType, scopeId)),
            KnowledgeScopeType.Company => ResolveCompany(companyId, scopeType, scopeId),
            _ => throw GeneralException.ValidationError($"ไม่รู้จัก ScopeType: {scopeType}"),
        };

    public void EnsureValidScope(string companyId, string scopeType, string? scopeId)
    {
        switch (scopeType)
        {
            case KnowledgeScopeType.Lesson:
                GetLessonOrThrow(scopeId);
                break;
            case KnowledgeScopeType.Category:
                var category = knowledgeCategoryRepository.Get(RequireScopeId(scopeType, scopeId))
                    ?? throw GeneralException.NotFound("หมวดความรู้");
                if (category.Level != 2)
                {
                    throw GeneralException.ValidationError("ต้องเลือกหมวดย่อย (ชั้นที่ 2) เท่านั้น");
                }
                break;
            case KnowledgeScopeType.Company:
                ResolveCompany(companyId, scopeType, scopeId);
                break;
            default:
                throw GeneralException.ValidationError($"ไม่รู้จัก ScopeType: {scopeType}");
        }
    }

    private LessonConfig GetLessonOrThrow(string? scopeId)
        => lessonConfigRepository.Get(RequireScopeId(KnowledgeScopeType.Lesson, scopeId))
            ?? throw GeneralException.NotFound("บทเรียน");

    private static string RequireScopeId(string scopeType, string? scopeId)
        => !string.IsNullOrEmpty(scopeId) ? scopeId : throw GeneralException.ValidationError($"ScopeType เป็น \"{scopeType}\" ต้องมี ScopeId");

    /// <summary>company scope forbids a ScopeId outright (KS-2: sent = rejected, not ignored) - a
    /// caller that silently dropped a stray ScopeId here could end up believing it scoped something
    /// to a category/lesson when it was actually saved company-wide.</summary>
    private static string ResolveCompany(string companyId, string scopeType, string? scopeId)
        => string.IsNullOrEmpty(scopeId)
            ? KnowledgeNamespaces.ForGlobal(companyId)
            : throw GeneralException.ValidationError($"ScopeType เป็น \"{scopeType}\" ต้องไม่มี ScopeId");
}

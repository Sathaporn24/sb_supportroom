using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Providers.Data.Data.UnitOfWork;

/// <summary>
/// Local equivalent of BossupStandard's UnitOfWork&lt;TDbContext&gt; (see .claude/skills/
/// dotnet-layered-backend/SKILL.md) - every new repository must be added to Register below,
/// or GetRepository&lt;TRepository&gt;() throws for it.
/// </summary>
public sealed class UnitOfWork(ApplicationDbContext dbContext, IServiceProvider serviceProvider) : IUnitOfWork
{
    private static readonly Dictionary<Type, Type> Register = new()
    {
        { typeof(ICompanyRepository), typeof(CompanyRepository) },
        { typeof(IAdminUserRepository), typeof(AdminUserRepository) },
        { typeof(ITrainingLinkRepository), typeof(TrainingLinkRepository) },
        { typeof(ILearningSessionRepository), typeof(LearningSessionRepository) },
        { typeof(ISessionQuestionRepository), typeof(SessionQuestionRepository) },
        { typeof(ILessonConfigRepository), typeof(LessonConfigRepository) },
        { typeof(IDocumentResourceRepository), typeof(DocumentResourceRepository) },
        { typeof(IKnowledgeCategoryRepository), typeof(KnowledgeCategoryRepository) },
        { typeof(IBackgroundJobRepository), typeof(BackgroundJobRepository) },
        { typeof(IDocumentChunkRepository), typeof(DocumentChunkRepository) },
        { typeof(ILessonSlideNarrationRepository), typeof(LessonSlideNarrationRepository) },
        { typeof(IKnowledgeQnARepository), typeof(KnowledgeQnARepository) },
        { typeof(IKnowledgeQnASourceRepository), typeof(KnowledgeQnASourceRepository) },
        { typeof(IKnowledgeQnAConflictRepository), typeof(KnowledgeQnAConflictRepository) },
    };

    public TRepository GetRepository<TRepository>()
    {
        if (!Register.TryGetValue(typeof(TRepository), out var implementationType))
        {
            throw new InvalidOperationException($"No repository registered for {typeof(TRepository).Name} - add it to UnitOfWork.Register.");
        }

        return (TRepository)ActivatorUtilities.CreateInstance(serviceProvider, implementationType);
    }

    public void Commit() => dbContext.SaveChanges();
}

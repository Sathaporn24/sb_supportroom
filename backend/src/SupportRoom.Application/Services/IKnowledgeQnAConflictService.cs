using Mapster;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.Exceptions;
using SupportRoom.Application.ViewModel;
using SupportRoom.Providers.Data.Data.UnitOfWork;
using SupportRoom.Providers.Data.Repository;

namespace SupportRoom.Application.Services;

public interface IKnowledgeQnAConflictService
{
    /// <summary>QQ-10 - the conflict flags page, always the open ones: closed flags are not shown
    /// anywhere, there is nothing left for CS to act on once ResolvedAt is set.</summary>
    IReadOnlyList<KnowledgeQnAConflictViewModel> GetUnresolved();

    Task<KnowledgeQnAConflictViewModel> ResolveAsync(string id);
}

public sealed class KnowledgeQnAConflictService(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<IKnowledgeQnAConflictService> logger)
    : ServiceBase<IKnowledgeQnAConflictService>(unitOfWork, serviceProvider, logger), IKnowledgeQnAConflictService
{
    private readonly IKnowledgeQnAConflictRepository _repository = unitOfWork.GetRepository<IKnowledgeQnAConflictRepository>();

    public IReadOnlyList<KnowledgeQnAConflictViewModel> GetUnresolved()
        => _repository.GetUnresolved().OrderBy(x => x.CreateDate).ToList().Adapt<List<KnowledgeQnAConflictViewModel>>();

    public Task<KnowledgeQnAConflictViewModel> ResolveAsync(string id)
    {
        var entity = _repository.Get(id) ?? throw GeneralException.NotFound("ธงขัดแย้ง");

        // QQ-10 - closed with an explicit action (CS confirming they fixed the source document),
        // never automatically.
        entity.ResolvedAt = DateTime.UtcNow;
        entity.ResolvedBy = CurrentUserId;
        _repository.Update(entity);
        UnitOfWork.Commit();

        Logger.LogInformation("Q&A conflict resolved: {ConflictId} by={ActorId}", id, CurrentUserId);

        return Task.FromResult(entity.Adapt<KnowledgeQnAConflictViewModel>());
    }
}

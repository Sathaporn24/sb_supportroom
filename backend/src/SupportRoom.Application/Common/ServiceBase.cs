using Microsoft.Extensions.Logging;
using SupportRoom.Providers.Data.Data.UnitOfWork;

namespace SupportRoom.Application.Common;

/// <summary>
/// Local equivalent of BossupStandard's ServiceBase&lt;TServiceInterface, TDbContext&gt; (see
/// .claude/skills/dotnet-layered-backend/SKILL.md). Business logic, validation, GeneralException
/// throws, mapping, and _unitOfWork.Commit() all belong in classes derived from this.
/// </summary>
public abstract class ServiceBase<TServiceInterface>(
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider,
    ILogger<TServiceInterface> logger)
{
    protected readonly IUnitOfWork UnitOfWork = unitOfWork;
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly ILogger<TServiceInterface> Logger = logger;
}

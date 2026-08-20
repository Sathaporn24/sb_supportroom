using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportRoom.Application.Exceptions;
using SupportRoom.Domain.Common;
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

    /// <summary>
    /// The company this request belongs to. Resolved from a request scope, so every service gets
    /// it without threading it through constructors.
    /// </summary>
    protected ICompanyContext CompanyContext { get; } = serviceProvider.GetRequiredService<ICompanyContext>();

    /// <summary>
    /// Company id to stamp on rows being created. Throws rather than defaulting: a row written
    /// without a company would be invisible to every subsequent query (the filter would never
    /// match it), which is a far more confusing failure than an outright error here.
    /// </summary>
    protected string CurrentCompanyId => CompanyContext.CompanyId
        ?? throw GeneralException.ConfigError("ยังไม่ทราบบริษัทของคำขอนี้");

    /// <summary>
    /// Who to stamp into CreateBy/UpdateBy, or null when nobody is signed in.
    ///
    /// Null is the correct, expected value on the learner surface: those rows are created by
    /// someone with no account, and RecipientName already records who they said they were. That
    /// makes this usable without branching - assign it unconditionally and it records the admin
    /// when there is one, and nothing when the learner acted.
    ///
    /// Unlike CurrentCompanyId this never throws: an unattributed row is a small gap in the audit
    /// trail, not a row that vanishes from every query.
    /// </summary>
    protected string? CurrentUserId => ServiceProvider.GetService<ICurrentUser>()?.UserId;
}

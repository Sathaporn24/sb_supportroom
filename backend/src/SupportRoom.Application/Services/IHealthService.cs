using Microsoft.Extensions.Logging;
using SupportRoom.Application.Common;
using SupportRoom.Application.ViewModel;
using SupportRoom.Domain.Configuration;
using SupportRoom.Providers.Data.Data.UnitOfWork;

namespace SupportRoom.Application.Services;

public interface IHealthService
{
    HealthViewModel Get();
}

public sealed class HealthService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<IHealthService> logger)
    : ServiceBase<IHealthService>(unitOfWork, serviceProvider, logger), IHealthService
{
    public HealthViewModel Get()
    {
        var providers = ProviderSelectionReader.Read();
        return new HealthViewModel
        {
            Status = "ok",
            Providers = new HealthProvidersViewModel
            {
                SlidesProvider = providers.SlidesProvider,
                TtsProvider = providers.TtsProvider,
                VoiceQuestionProvider = providers.VoiceQuestionProvider,
            },
            Timestamp = DateTime.UtcNow.ToString("O"),
        };
    }
}

using Microsoft.Extensions.Logging;

namespace SupportRoom.Providers.Storage;

public static class DocumentStorageProviderFactory
{
    public static IDocumentStorageProvider Create(string documentStorageProvider, ILoggerFactory loggerFactory) => documentStorageProvider switch
    {
        SupportRoom.Domain.Configuration.DocumentStorageProvider.HuaweiObs => new HuaweiObsDocumentStorageProvider(loggerFactory.CreateLogger<HuaweiObsDocumentStorageProvider>()),
        SupportRoom.Domain.Configuration.DocumentStorageProvider.Local => new LocalDocumentStorageProvider(loggerFactory.CreateLogger<LocalDocumentStorageProvider>()),
        // Unreachable in practice - ProviderSelectionReader already validates against
        // DocumentStorageProvider.Allowed before this value ever reaches here.
        _ => throw new ArgumentOutOfRangeException(nameof(documentStorageProvider), documentStorageProvider, "Unknown document storage provider"),
    };
}

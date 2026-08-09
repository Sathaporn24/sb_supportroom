using Microsoft.Extensions.Logging;
using SupportRoom.Api.Realtime;
using SupportRoom.Api;
using SupportRoom.Application.Common;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Configuration;
using SupportRoom.Providers.Knowledge;
using SupportRoom.Providers.Slides;
using SupportRoom.Providers.Storage;
using SupportRoom.Providers.Tts;
using SupportRoom.Providers.VoiceQuestion;

namespace SupportRoom.Api.Configurations;

public static class ServiceConfiguration
{
    public static IServiceCollection AddServiceConfiguration(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<ITrainingSessionService, TrainingSessionService>();
        services.AddScoped<ISessionQuestionService, SessionQuestionService>();
        services.AddScoped<ISessionSummaryService, SessionSummaryService>();
        services.AddScoped<ILessonConfigService, LessonConfigService>();
        services.AddScoped<ISlidesService, SlidesService>();
        services.AddScoped<ITtsService, TtsService>();
        services.AddScoped<IVoiceQuestionService, VoiceQuestionService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IChatMessageService, ChatMessageService>();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
        services.AddScoped<IKnowledgeIndexingService, KnowledgeIndexingService>();
        services.AddScoped<IDocumentResourceService, DocumentResourceService>();

        // External API calls go through HttpClientFactory-managed clients (see .claude/skills/
        // dotnet-layered-backend/SKILL.md "External API calls").
        services.AddHttpClient();

        // Short-lived cache for downloaded PDF bytes, parsed slide content, and rendered page
        // images (LessonConfigService) so opening/viewing a PDF room doesn't redo that work once
        // per request.
        services.AddMemoryCache();

        // Document upload (DocumentResourceService.UploadAsync) enqueues the slow part (text
        // extraction, embedding, Pinecone upsert) here instead of doing it inline, so the upload
        // response returns as soon as the file is stored - see QueuedHostedService for the drain
        // loop and IBackgroundTaskQueue's doc comment for the tradeoffs of an in-memory queue.
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<QueuedHostedService>();

        // Providers select among Real implementations per SupportRoom.Domain.Configuration.
        // ProviderSelectionReader (mirrors src/providers/*/index.ts's createXProvider() factories)
        // - every category requires an explicit, valid env var; there is no Mock fallback.
        var selection = ProviderSelectionReader.Read();
        services.AddScoped<ISlidesProvider>(sp => SlidesProviderFactory.Create(selection.SlidesProvider, sp.GetRequiredService<ILoggerFactory>()));
        services.AddScoped<ITtsProvider>(sp => TtsProviderFactory.Create(
            selection.TtsProvider, sp.GetRequiredService<ILoggerFactory>(), sp.GetRequiredService<IHttpClientFactory>()));
        services.AddScoped(sp => KnowledgeProviderFactory.Create(
            selection.KnowledgeProvider, sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<ILoggerFactory>()));
        services.AddScoped<IEmbeddingProvider>(sp => sp.GetRequiredService<KnowledgeProviders>().Embedding);
        services.AddScoped<IKnowledgeIndexProvider>(sp => sp.GetRequiredService<KnowledgeProviders>().Index);
        services.AddScoped<IVoiceQuestionProvider>(sp =>
            VoiceQuestionProviderFactory.Create(
                selection.VoiceQuestionProvider,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IEmbeddingProvider>(),
                sp.GetRequiredService<IKnowledgeIndexProvider>(),
                sp.GetRequiredService<ILoggerFactory>()));
        services.AddScoped<IDocumentStorageProvider>(sp => DocumentStorageProviderFactory.Create(selection.DocumentStorageProvider, sp.GetRequiredService<ILoggerFactory>()));

        return services;
    }
}

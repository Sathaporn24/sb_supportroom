using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SupportRoom.Api.Realtime;
using SupportRoom.Api;
using SupportRoom.Application.Common;
using SupportRoom.Application.Realtime;
using SupportRoom.Application.Services;
using SupportRoom.Domain.Common;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Entities;
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
        // Request-scoped identity. ICurrentUser is resolved by CurrentUserMiddleware from a
        // verified JWT; both start out unresolved so an anonymous request reads as "nobody"
        // rather than as anyone trusted.
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuthorizationGuard, AuthorizationGuard>();
        // Only the hasher is taken from Identity, not the full stack - see the package comment in
        // SupportRoom.Application.csproj.
        services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<ITrainingLinkService, TrainingLinkService>();
        services.AddScoped<ILearningSessionService, LearningSessionService>();
        services.AddScoped<ISessionQuestionService, SessionQuestionService>();
        services.AddScoped<ILessonConfigService, LessonConfigService>();
        services.AddScoped<ISlidesService, SlidesService>();
        services.AddScoped<ITtsService, TtsService>();
        services.AddScoped<IVoiceQuestionService, VoiceQuestionService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IChatMessageService, ChatMessageService>();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
        services.AddScoped<IKnowledgeIndexingService, KnowledgeIndexingService>();
        services.AddScoped<IDocumentResourceService, DocumentResourceService>();
        services.AddScoped<IKnowledgeCategoryService, KnowledgeCategoryService>();
        services.AddScoped<IKnowledgeNamespaceResolver, KnowledgeNamespaceResolver>();
        services.AddScoped<IBackgroundJobProcessor, BackgroundJobProcessor>();
        services.AddScoped<ILessonSlideNarrationResolver, LessonSlideNarrationResolver>();
        services.AddScoped<ILessonSlideNarrationService, LessonSlideNarrationService>();
        services.AddScoped<IKnowledgeQnAService, KnowledgeQnAService>();
        services.AddScoped<IKnowledgeQnAConflictService, KnowledgeQnAConflictService>();

        // External API calls go through HttpClientFactory-managed clients (see .claude/skills/
        // dotnet-layered-backend/SKILL.md "External API calls").
        services.AddHttpClient();

        // Short-lived cache for downloaded PDF bytes, parsed slide content, and rendered page
        // images (LessonConfigService) so opening/viewing a PDF room doesn't redo that work once
        // per request.
        services.AddMemoryCache();

        // Document upload (DocumentResourceService.UploadAsync) enqueues a BackgroundJob row
        // instead of doing the slow part (text extraction, embedding, Pinecone upsert) inline, so
        // the upload response returns as soon as the file is stored - BackgroundJobHostedService
        // polls for and runs those jobs, surviving a restart (design.md DI-1..DI-17), unlike the
        // in-memory IBackgroundTaskQueue/QueuedHostedService this replaced.
        services.AddHostedService<BackgroundJobHostedService>();

        // Creates the first owner account when there are none at all - otherwise a fresh
        // deployment has nobody who can sign in to create one.
        services.AddHostedService<SeedFirstOwnerHostedService>();

        // Creates the first company (School Bright by default) when the registry is completely
        // empty - otherwise the seeded owner above signs in with nothing to switch to and every
        // company-scoped screen is a dead end.
        services.AddHostedService<SeedFirstCompanyHostedService>();

        // Providers select among Real implementations per SupportRoom.Domain.Configuration.
        // ProviderSelectionReader (mirrors src/providers/*/index.ts's createXProvider() factories)
        // - every category requires an explicit, valid env var; there is no Mock fallback.
        var selection = ProviderSelectionReader.Read();
        services.AddScoped<ISlidesProvider>(sp => SlidesProviderFactory.Create(selection.SlidesProvider, sp.GetRequiredService<ILoggerFactory>()));
        services.AddScoped<ITtsProvider>(sp => TtsProviderFactory.Create(
            selection.TtsProvider, sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<ILoggerFactory>()));
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

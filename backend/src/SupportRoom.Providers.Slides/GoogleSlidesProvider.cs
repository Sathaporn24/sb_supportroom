using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain;
using SupportRoom.Domain.Configuration;

namespace SupportRoom.Providers.Slides;

/// <summary>
/// Real Google Slides integration - service-account JWT auth, presentations.readonly scope.
/// Mirrors src/providers/slides/google-slides-provider.ts exactly (notes extraction, URL
/// parse failure states).
/// </summary>
public sealed class GoogleSlidesProvider(ILogger<GoogleSlidesProvider> logger) : ISlidesProvider
{
    private static SlidesService BuildClient()
    {
        var creds = ExternalServiceEnv.GetGoogleServiceAccount();
        var credential = new ServiceAccountCredential(
            new ServiceAccountCredential.Initializer(creds.ClientEmail)
            {
                ProjectId = creds.ProjectId,
                Scopes = ["https://www.googleapis.com/auth/presentations.readonly"],
            }.FromPrivateKey(creds.PrivateKey));

        return new SlidesService(new BaseClientService.Initializer { HttpClientInitializer = credential });
    }

    private static string ExtractSpeakerNotes(Page slide)
    {
        var notesElements = slide.SlideProperties?.NotesPage?.PageElements ?? [];
        foreach (var element in notesElements)
        {
            if (element.Shape?.Placeholder?.Type != "BODY")
            {
                continue;
            }

            var textElements = element.Shape.Text?.TextElements ?? [];
            var text = string.Concat(textElements.Select(run => run.TextRun?.Content ?? ""));
            return text.Trim();
        }
        return "";
    }

    public async Task<ResolvedPresentation> ResolvePresentationAsync(ResolvePresentationInput input)
    {
        if (!GoogleSlidesUrl.IsValid(input.SlidesSourceUrl))
        {
            return new ResolvedPresentation
            {
                PresentationId = null,
                EmbedUrl = input.SlidesEmbedUrl ?? "",
                IsEmbedOnly = false,
                Warning = "URL ที่วางไม่ใช่ลิงก์ Google Slides ที่ถูกต้อง กรุณาวางลิงก์จาก docs.google.com/presentation/...",
            };
        }

        var parsed = GoogleSlidesUrl.Parse(input.SlidesSourceUrl);
        if (parsed.PresentationId is null)
        {
            return new ResolvedPresentation
            {
                PresentationId = null,
                EmbedUrl = input.SlidesEmbedUrl ?? input.SlidesSourceUrl,
                IsEmbedOnly = true,
                Warning = "ไม่สามารถอ่าน presentationId จาก URL นี้ได้ (อาจเป็นลิงก์ Published) จะใช้แสดงผลได้อย่างเดียว " +
                    "กรุณาวาง Source URL แบบ /presentation/d/<id>/edit เพื่ออ่าน Speaker Notes",
            };
        }

        var slides = BuildClient();
        // Fetching minimal metadata confirms the service account has Viewer access.
        var getRequest = slides.Presentations.Get(parsed.PresentationId);
        getRequest.Fields = "presentationId";
        await TimedCallAsync("presentations.get.metadata", () => getRequest.ExecuteAsync());

        return new ResolvedPresentation
        {
            PresentationId = parsed.PresentationId,
            EmbedUrl = input.SlidesEmbedUrl ?? GoogleSlidesUrl.BuildEmbedUrl(parsed.PresentationId),
            IsEmbedOnly = false,
        };
    }

    public async Task<SlidesLessonContent> GetLessonContentAsync(GetLessonContentInput input)
    {
        var slidesService = BuildClient();
        var presentation = await TimedCallAsync("presentations.get", () => slidesService.Presentations.Get(input.PresentationId).ExecuteAsync());
        var slideList = presentation.Slides ?? [];

        return new SlidesLessonContent
        {
            PresentationId = input.PresentationId,
            Title = presentation.Title ?? "",
            EmbedUrl = GoogleSlidesUrl.BuildEmbedUrl(input.PresentationId),
            Slides = slideList.Select((slide, index) => new ResolvedSlide
            {
                SlideObjectId = slide.ObjectId ?? $"slide-{index}",
                Index = index,
                SpeakerNotes = ExtractSpeakerNotes(slide),
            }).ToList(),
            SyncedAt = DateTime.UtcNow.ToString("O"),
        };
    }

    private async Task<T> TimedCallAsync<T>(string operation, Func<Task<T>> call)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await call();
            logger.LogInformation("Provider call succeeded: {Provider} {Operation} in {ElapsedMs}ms", "google-slides", operation, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Provider call failed: {Provider} {Operation} after {ElapsedMs}ms - {Error}",
                "google-slides", operation, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}

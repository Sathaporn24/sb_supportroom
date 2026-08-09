using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Enums;
using SupportRoom.Providers.Knowledge;

namespace SupportRoom.Providers.VoiceQuestion;

/// <summary>
/// Same IVoiceQuestionProvider contract as GeminiVoiceQuestionProvider - retrieval-grounded
/// instead of full-deck-context, so zero frontend/controller changes are needed to switch.
///
/// Three round trips instead of one: retrieval needs the question as text to embed and search,
/// but the only text available up front is the raw audio, so the question has to be transcribed
/// before anything relevant can be found. (1) transcribe-only (small/fast prompt) -> (2) embed
/// the transcript, query the knowledge store for the top-K relevant slides -> (3) answer
/// (text-only prompt, just those slides' notes). Steps 1 and 3 are each smaller than
/// GeminiVoiceQuestionProvider's single call (which already carries the full deck as text
/// alongside the audio), so this is a net latency win as a deck grows - the whole point of RAG.
///
/// If embedding/retrieval fails or returns nothing (e.g. the lesson was never indexed), falls
/// back to sending the full deck for that one question - a retrieval outage must never break a
/// live demo, it should just silently behave like the older provider for that question.
/// </summary>
public sealed class RagVoiceQuestionProvider(
    IHttpClientFactory httpClientFactory,
    IEmbeddingProvider embeddingProvider,
    IKnowledgeIndexProvider knowledgeIndexProvider,
    ILogger<RagVoiceQuestionProvider> logger) : IVoiceQuestionProvider
{
    private static int TopK => int.TryParse(Environment.GetEnvironmentVariable("RAG_TOP_K"), out var k) && k > 0 ? k : 3;

    // Minimum cosine score a retrieved chunk must clear to count as "relevant". Pinecone always
    // returns the nearest K vectors, even for a question the deck has nothing to say about - without
    // a floor, those barely-related chunks get handed to the answer step as "relevant references",
    // pushing it to answer off-topic questions from unrelated slides instead of reporting not_found.
    // Conservative default (0.4): tune via RAG_MIN_SCORE using the scores logged on each retrieval.
    private static double MinScore =>
        double.TryParse(Environment.GetEnvironmentVariable("RAG_MIN_SCORE"), out var s) && s >= 0 ? s : 0.4;

    private static string BuildTranscribeOnlyPrompt() => string.Join('\n',
    [
        "ฟังไฟล์เสียงที่แนบมา ถอดเสียงเป็นข้อความภาษาไทยเท่านั้น ห้ามตอบคำถามหรือเดาคำตอบใด ๆ",
        "",
        "ตอบกลับเป็น JSON เท่านั้น ตาม schema:",
        """{"transcript": string}""",
    ]);

    private static string BuildReadinessPrompt() => string.Join('\n',
    [
        "ฟังไฟล์เสียงที่แนบมา คุณครูกำลังตอบคำถามว่า พร้อมเริ่มเรียนหรือยัง",
        "ถอดเสียงเป็นข้อความภาษาไทย แล้วตัดสินว่าคำตอบคือพร้อมหรือยังไม่พร้อม",
        """ถ้าฟังไม่ชัดหรือไม่แน่ใจ ให้ถือว่า "not_ready" เพื่อไม่ให้เริ่มเรียนโดยที่คุณครูยังไม่ได้ตั้งใจ""",
        "",
        "ตอบกลับเป็น JSON เท่านั้น ตาม schema:",
        """{"transcript": string, "readiness": "ready" | "not_ready"}""",
    ]);

    private static string BuildAnswerPrompt(string transcript, string groundingContext) => string.Join('\n',
    [
        "คุณคือผู้ช่วยตอบคำถามคุณครูระหว่างบทเรียนสาธิตการใช้งานระบบ",
        $"คำถามที่คุณครูถาม (ถอดเสียงมาแล้ว): {transcript}",
        "ตอบโดยอ้างอิงเฉพาะข้อมูลอ้างอิงที่เกี่ยวข้องกับคำถามนี้ด้านล่างเท่านั้น",
        "ห้ามตอบจากความรู้ทั่วไป ห้ามเดาคำตอบ ตอบสั้นและกระชับ",
        "ตอบในน้ำเสียงธรรมชาติเหมือนติวเตอร์ที่จำเนื้อหาได้ ไม่ใช่อ่านสคริปต์ตรง ๆ",
        "",
        "ข้อมูลอ้างอิงที่เกี่ยวข้องกับคำถามนี้:",
        groundingContext,
        "",
        "ตอบกลับเป็น JSON เท่านั้น ตาม schema:",
        """{"answer": string, "answerStatus": "answered" | "not_found" | "out_of_scope", "relatedSlideObjectId": string | null}""",
        "",
        "relatedSlideObjectId: เมื่อ answerStatus = answered ให้ใส่ objectId ของข้อมูลอ้างอิงที่ใช้ตอบเสมอ",
        "ต้องเป็น objectId ที่ปรากฏในรายการด้านบนเท่านั้น ห้ามสร้างขึ้นเอง",
        "ถ้าตอบไม่ได้ ให้เป็น null",
    ]);

    public async Task<VoiceQuestionResult> TranscribeAndAnswerAsync(VoiceQuestionInput input)
    {
        if (input.DurationMs < UploadLimits.MinVoiceDurationMs)
        {
            return new VoiceQuestionResult { AnswerStatus = AnswerStatus.NoSpeech };
        }

        var creds = ExternalServiceEnv.GetGemini();

        if (input.Expecting == "readiness")
        {
            // No grounding needed for a yes/no reply - identical to the full-context provider.
            var readiness = await CallAndParseAsync(creds, BuildReadinessPrompt(), input.Audio, input.MimeType);
            if (readiness is null)
            {
                return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
            }
            return new VoiceQuestionResult
            {
                Transcript = readiness.Transcript ?? "",
                AnswerStatus = AnswerStatus.Answered,
                Readiness = readiness.Readiness == "ready" ? "ready" : "not_ready",
            };
        }

        // Step 1: transcribe only.
        var transcribed = await CallAndParseAsync(creds, BuildTranscribeOnlyPrompt(), input.Audio, input.MimeType);
        if (transcribed is null || string.IsNullOrEmpty(transcribed.Transcript))
        {
            return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
        }
        var transcript = transcribed.Transcript;

        // Step 2: embed + retrieve the top-K relevant slides (falls back to the full deck below
        // if this fails or the lesson isn't indexed yet).
        var groundingContext = await BuildGroundingContextAsync(input, transcript);

        // Step 3: answer using only the retrieved (or fallback full-deck) context.
        var answered = await CallAndParseAsync(creds, BuildAnswerPrompt(transcript, groundingContext));
        if (answered is null || !GeminiRest.IsAnswerStatus(answered.AnswerStatus))
        {
            return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
        }

        return new VoiceQuestionResult
        {
            Transcript = transcript,
            Answer = answered.Answer ?? "",
            AnswerStatus = answered.AnswerStatus!,
            RelatedSlideObjectId = answered.RelatedSlideObjectId,
        };
    }

    private async Task<string> BuildGroundingContextAsync(VoiceQuestionInput input, string transcript)
    {
        try
        {
            var queryVector = await embeddingProvider.EmbedAsync(transcript, EmbeddingTaskType.RetrievalQuery);

            // Every question is grounded against this lesson's own namespace *and* the shared
            // "kb-global" namespace - a CS-uploaded standalone document (no lessonSlug at
            // upload time) must answer questions in any lesson, not just one it happens to be
            // tagged to. Querying kb-global for a lesson that's also literally named "kb-global"
            // is impossible (lesson slugs come from LessonConfig, kb-global is a reserved key),
            // so there's no risk of double-counting the same namespace.
            var lessonChunksTask = knowledgeIndexProvider.QueryAsync(input.LessonSlug, queryVector, TopK);
            var globalChunksTask = knowledgeIndexProvider.QueryAsync(KnowledgeNamespaces.Global, queryVector, TopK);
            await Task.WhenAll(lessonChunksTask, globalChunksTask);

            var allMatches = MergeTopK(lessonChunksTask.Result, globalChunksTask.Result, TopK);

            // Nothing in either namespace means this lesson was never indexed (a fresh deck, or a
            // retrieval outage) - that's the case the full-deck fallback below exists for. An
            // off-topic question against an indexed deck is different: it DOES return matches, just
            // low-scoring ones, and must NOT fall back to the whole deck (see the threshold branch).
            var indexedAtAll = lessonChunksTask.Result.Count > 0 || globalChunksTask.Result.Count > 0;
            var relevant = allMatches.Where(c => c.Score >= MinScore).ToList();

            logger.LogInformation(
                "Retrieval for lesson {LessonSlug}: {LessonMatchCount} lesson + {GlobalMatchCount} kb-global matches, top score {TopScore:F3}, {RelevantCount} cleared threshold {MinScore:F2}, using [{ChunkIds}]",
                input.LessonSlug, lessonChunksTask.Result.Count, globalChunksTask.Result.Count,
                allMatches.Count > 0 ? allMatches.Max(c => c.Score) : 0f, relevant.Count, MinScore,
                string.Join(", ", relevant.Select(c => c.Id)));

            if (relevant.Count > 0)
            {
                return string.Join('\n', relevant.Select(c =>
                {
                    var slideId = c.Metadata is not null && c.Metadata.TryGetValue("slideObjectId", out var id) ? id : c.Id;
                    return $"({slideId}): {c.Text}";
                }));
            }

            if (indexedAtAll)
            {
                // The deck is indexed but nothing scored above the threshold - the question just
                // isn't covered. Hand the answer step an explicit "no relevant reference" instead
                // of the full deck, so it reports not_found rather than answering from unrelated
                // slides. (The prompt already treats an empty/irrelevant context as not_found.)
                return "(ไม่พบข้อมูลอ้างอิงที่เกี่ยวข้องกับคำถามนี้)";
            }
        }
        catch (Exception ex)
        {
            // fall through to the full-deck fallback below - a retrieval outage must never break
            // a live demo, but it should still be visible in the logs, not silent.
            logger.LogWarning(ex, "Retrieval fell back to full-deck context for lesson {LessonSlug}: {Error}", input.LessonSlug, ex.Message);
        }

        return string.Join('\n', input.LessonSlides.Select((slide, index) => $"Slide {index + 1} ({slide.SlideObjectId}): {slide.SpeakerNotes}"));
    }

    /// <summary>Pulled out as its own pure function (no embedding/HTTP calls) so the
    /// lesson-namespace + kb-global merge-and-rank behavior is directly unit-testable without a
    /// live Gemini call.</summary>
    public static IReadOnlyList<ScoredChunk> MergeTopK(IReadOnlyList<ScoredChunk> lessonChunks, IReadOnlyList<ScoredChunk> globalChunks, int topK)
        => lessonChunks.Concat(globalChunks).OrderByDescending(c => c.Score).Take(topK).ToList();

    private async Task<GeminiRest.GeminiAnswerJson?> CallAndParseAsync(
        GeminiCredentials creds, string prompt, byte[]? audio = null, string? mimeType = null)
    {
        var text = await GeminiRest.CallAsync(httpClientFactory, creds, logger, prompt, audio, mimeType);
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<GeminiRest.GeminiAnswerJson>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

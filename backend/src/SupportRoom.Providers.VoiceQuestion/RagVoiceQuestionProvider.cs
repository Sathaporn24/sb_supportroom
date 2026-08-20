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
    ILogger<RagVoiceQuestionProvider> logger,
    bool useOpenAiAnswer = false) : IVoiceQuestionProvider
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

    /// <summary>KS-7/KS-8/KS-9 - two separate blocks, never one merged-and-ranked list: the model
    /// has to be able to tell "this came from a document/slide" apart from "this came from a Q&A"
    /// to know which one wins when they disagree (KS-7 - documentBlock always wins), and to avoid
    /// copying a Q&A's answer verbatim (KS-8). This is a prompt-level rule only, not something code
    /// can enforce - the model decides for itself what counts as "conflicting" (design.md R-3).</summary>
    private static string BuildAnswerPrompt(string transcript, string documentBlock, string qnaBlock) => string.Join('\n',
    [
        "คุณคือผู้ช่วยตอบคำถามคุณครูระหว่างบทเรียนสาธิตการใช้งานระบบ",
        $"คำถามที่คุณครูถาม (ถอดเสียงมาแล้ว): {transcript}",
        "ตอบโดยอ้างอิงเฉพาะข้อมูลอ้างอิงที่เกี่ยวข้องกับคำถามนี้ด้านล่างเท่านั้น",
        "ห้ามตอบจากความรู้ทั่วไป ห้ามเดาคำตอบ ตอบสั้นและกระชับ",
        "ตอบในน้ำเสียงธรรมชาติเหมือนติวเตอร์ที่จำเนื้อหาได้ ไม่ใช่อ่านสคริปต์ตรง ๆ",
        "",
        "=== บล็อกที่ 1: เอกสาร/สไลด์ (แหล่งข้อมูลหลัก) ===",
        documentBlock,
        "",
        "=== บล็อกที่ 2: คำถาม-คำตอบที่ทีมงานเคยเขียนไว้ (ใช้เป็นแนวทางเท่านั้น) ===",
        qnaBlock,
        "",
        "กติกาการใช้ข้อมูลสองบล็อกด้านบน:",
        "1. ถ้าบล็อกที่ 1 และบล็อกที่ 2 ขัดแย้งกัน ให้ยึดบล็อกที่ 1 (เอกสาร/สไลด์) เป็นคำตอบเสมอ แล้วรายงานความขัดแย้งผ่านฟิลด์ conflict",
        "2. ห้ามคัดลอกข้อความจากบล็อกที่ 2 มาตอบตรง ๆ - ให้เรียบเรียงคำตอบใหม่ด้วยคำพูดของคุณเอง",
        "3. คำถามที่ใกล้เคียงกันในเชิงภาษาอาจเป็นคนละเรื่องในสาระ เช่น \"ลบข้อมูลนักเรียนยังไง\" กับ \"ลบข้อมูลนักเรียนที่จบไปแล้วยังไง\" - ถ้าคำถาม-คำตอบในบล็อกที่ 2 ไม่ตรงกับคำถามจริงของคุณครู ให้ตอบ not_found แม้จะมีคำถาม-คำตอบที่หน้าตาคล้ายกันอยู่ก็ตาม",
        "",
        "ตอบกลับเป็น JSON เท่านั้น ตาม schema:",
        """{"answer": string, "answerStatus": "answered" | "not_found" | "out_of_scope", "relatedSlideObjectId": string | null, "conflict": {"qnaId": string, "sourceLabel": string, "note": string} | null}""",
        "",
        "relatedSlideObjectId: เมื่อ answerStatus = answered และใช้บล็อกที่ 1 ตอบ ให้ใส่ objectId ที่ปรากฏในบล็อกที่ 1 เท่านั้น ห้ามสร้างขึ้นเอง ถ้าตอบไม่ได้หรือใช้บล็อกที่ 2 ตอบ ให้เป็น null",
        "",
        "conflict: ใส่เฉพาะเมื่อบล็อกที่ 2 มีคำถาม-คำตอบที่ขัดแย้งกับบล็อกที่ 1 จริง (ไม่ใช่แค่พูดถึงเรื่องเดียวกัน) - qnaId คือรหัสในวงเล็บนำหน้าคำถาม-คำตอบนั้นในบล็อกที่ 2, sourceLabel คือชื่อแหล่งข้อมูลในบล็อกที่ 1 ที่ขัดแย้งด้วย, note คือคำอธิบายสั้น ๆ ว่าขัดกันตรงไหน ถ้าไม่มีความขัดแย้งให้เป็น null",
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
        var grounding = await BuildGroundingContextAsync(input, transcript);

        // Step 3: answer using only the retrieved (or fallback full-deck) context. Optionally
        // offloaded to OpenAI (keeps the heavy answer generation off Gemini's quota) - transcription
        // above always stays on Gemini regardless.
        var answered = useOpenAiAnswer
            ? await AnswerWithOpenAiAsync(transcript, grounding.DocumentBlock, grounding.QnaBlock)
            : await CallAndParseAsync(creds, BuildAnswerPrompt(transcript, grounding.DocumentBlock, grounding.QnaBlock));
        if (answered is null || !GeminiRest.IsAnswerStatus(answered.AnswerStatus))
        {
            return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
        }

        // KS-10 - the id is not validated here (this provider has no repository access to check
        // it against); the caller (VoiceQuestionService) does that before ever writing a
        // KnowledgeQnAConflict row.
        var conflict = !string.IsNullOrEmpty(answered.Conflict?.QnaId)
            ? new VoiceQuestionConflictResult
            {
                QnAId = answered.Conflict!.QnaId!,
                SourceLabel = answered.Conflict.SourceLabel ?? "",
                Note = answered.Conflict.Note,
            }
            : null;

        return new VoiceQuestionResult
        {
            Transcript = transcript,
            Answer = answered.Answer ?? "",
            AnswerStatus = answered.AnswerStatus!,
            RelatedSlideObjectId = answered.RelatedSlideObjectId,
            Conflict = conflict,
        };
    }

    /// <summary>KS-7 - the two blocks BuildAnswerPrompt needs, kept apart by sourceType all the way
    /// from retrieval through to the prompt so the model always knows which is which.</summary>
    private sealed record GroundingBlocks(string DocumentBlock, string QnaBlock);

    private async Task<GroundingBlocks> BuildGroundingContextAsync(VoiceQuestionInput input, string transcript)
    {
        try
        {
            var queryVector = await embeddingProvider.EmbedAsync(transcript, EmbeddingTaskType.RetrievalQuery);

            // Every question is grounded against this lesson's own namespace, the namespace of the
            // knowledge category it belongs to (KS-3), and this company's shared standalone-document
            // namespace - a CS-uploaded standalone document (no lessonSlug at upload time) must
            // answer questions in any lesson, not just one it happens to be tagged to. All three keys
            // arrive already company-scoped from the caller (KnowledgeNamespaces.For / ForCategory /
            // ForGlobal); this provider must not build them itself (KS-1) - the global one in
            // particular is queried on every single question, and an unscoped key there would pull
            // another company's documents into this answer.
            var lessonChunksTask = knowledgeIndexProvider.QueryAsync(input.LessonNamespace, queryVector, TopK);
            var categoryChunksTask = knowledgeIndexProvider.QueryAsync(input.CategoryNamespace, queryVector, TopK);
            var globalChunksTask = knowledgeIndexProvider.QueryAsync(input.GlobalNamespace, queryVector, TopK);
            await Task.WhenAll(lessonChunksTask, categoryChunksTask, globalChunksTask);

            var allMatches = MergeTopK([lessonChunksTask.Result, categoryChunksTask.Result, globalChunksTask.Result], TopK);

            // Nothing in any of the three namespaces means this lesson/category was never indexed (a
            // fresh deck, or a retrieval outage) - that's the case the full-deck fallback below exists
            // for (KS-11: an empty/never-created namespace must never throw, just behave as if this
            // provider found nothing there). An off-topic question against an indexed deck is
            // different: it DOES return matches, just low-scoring ones, and must NOT fall back to the
            // whole deck (see the threshold branch).
            var indexedAtAll = lessonChunksTask.Result.Count > 0 || categoryChunksTask.Result.Count > 0 || globalChunksTask.Result.Count > 0;
            var relevant = allMatches.Where(c => c.Score >= MinScore).ToList();

            logger.LogInformation(
                "Retrieval for {LessonNamespace}: {LessonMatchCount} lesson + {CategoryMatchCount} category + {GlobalMatchCount} global matches, top score {TopScore:F3}, {RelevantCount} cleared threshold {MinScore:F2}, using [{ChunkIds}]",
                input.LessonNamespace, lessonChunksTask.Result.Count, categoryChunksTask.Result.Count, globalChunksTask.Result.Count,
                allMatches.Count > 0 ? allMatches.Max(c => c.Score) : 0f, relevant.Count, MinScore,
                string.Join(", ", relevant.Select(c => $"{c.Id}:{ResolveSourceType(c.Metadata)}")));

            if (relevant.Count > 0)
            {
                // KS-7 - split by sourceType into the two blocks the prompt keeps apart, never one
                // merged-and-ranked list.
                var documentChunks = relevant.Where(c => ResolveSourceType(c.Metadata) != KnowledgeSourceType.Qna).ToList();
                var qnaChunks = relevant.Where(c => ResolveSourceType(c.Metadata) == KnowledgeSourceType.Qna).ToList();

                var documentBlock = documentChunks.Count > 0
                    ? string.Join('\n', documentChunks.Select(c => $"({ResolveDisplayLabel(c)}): {c.Text}"))
                    : "(ไม่มีข้อมูลอ้างอิงจากเอกสาร/สไลด์ที่เกี่ยวข้องกับคำถามนี้)";
                var qnaBlock = qnaChunks.Count > 0
                    // c.Id here IS the qnaId - a Q&A's Pinecone vector id is its own row id (DM-6),
                    // so this is also what the model is told to echo back in conflict.qnaId.
                    ? string.Join('\n', qnaChunks.Select(c => $"(qnaId={c.Id}): {c.Text}"))
                    : "(ไม่มีคำถาม-คำตอบที่เกี่ยวข้องกับคำถามนี้)";

                return new GroundingBlocks(documentBlock, qnaBlock);
            }

            if (indexedAtAll)
            {
                // The deck is indexed but nothing scored above the threshold - the question just
                // isn't covered. Hand the answer step an explicit "no relevant reference" instead
                // of the full deck, so it reports not_found rather than answering from unrelated
                // slides. (The prompt already treats an empty/irrelevant context as not_found.)
                return new GroundingBlocks("(ไม่พบข้อมูลอ้างอิงที่เกี่ยวข้องกับคำถามนี้)", "(ไม่พบข้อมูลอ้างอิงที่เกี่ยวข้องกับคำถามนี้)");
            }
        }
        catch (Exception ex)
        {
            // fall through to the full-deck fallback below - a retrieval outage must never break
            // a live demo, but it should still be visible in the logs, not silent.
            logger.LogWarning(ex, "Retrieval fell back to full-deck context for {LessonNamespace}: {Error}", input.LessonNamespace, ex.Message);
        }

        // Full-deck fallback never carries Q&A content - Q&A only ever reaches this method through
        // retrieval, and a fallback happens precisely when retrieval could not be used.
        var fullDeck = string.Join('\n', input.LessonSlides.Select((slide, index) => $"Slide {index + 1} ({slide.SlideObjectId}): {slide.SpeakerNotes}"));
        return new GroundingBlocks(fullDeck, "(ไม่มีคำถาม-คำตอบที่เกี่ยวข้องกับคำถามนี้)");
    }

    /// <summary>DM-8's ConflictingSourceLabel needs something a person recognizes - a document's
    /// FileName, or "สไลด์หน้า N" - so the model is shown that label instead of a bare chunk id,
    /// which it is asked to echo back verbatim when reporting a conflict.</summary>
    private static string ResolveDisplayLabel(ScoredChunk chunk)
    {
        if (chunk.Metadata is null)
        {
            return chunk.Id;
        }
        if (chunk.Metadata.TryGetValue("fileName", out var fileName))
        {
            return fileName;
        }
        if (chunk.Metadata.TryGetValue("index", out var index))
        {
            return $"สไลด์หน้า {index}";
        }
        if (chunk.Metadata.TryGetValue("slideObjectId", out var slideId))
        {
            return slideId;
        }
        return chunk.Id;
    }

    /// <summary>Pulled out as its own pure function (no embedding/HTTP calls) so the
    /// lesson + category + kb-global merge-and-rank behavior is directly unit-testable without a
    /// live Gemini call. Takes any number of result sets so a future fourth namespace (see design.md
    /// KS-3's ParentCategoryNamespace note) is just one more list, not a signature change.</summary>
    public static IReadOnlyList<ScoredChunk> MergeTopK(IEnumerable<IReadOnlyList<ScoredChunk>> chunkSets, int topK)
        => chunkSets.SelectMany(c => c).OrderByDescending(c => c.Score).Take(topK).ToList();

    /// <summary>KS-6 - chunks indexed before metadata.sourceType existed have no such key at all;
    /// treated as Document (the only kind that existed before this field was introduced) rather
    /// than thrown away or erroring, so retrieval keeps working across the migration instead of
    /// silently losing pre-Phase-2 content.</summary>
    private static string ResolveSourceType(IReadOnlyDictionary<string, string>? metadata)
        => metadata is not null && metadata.TryGetValue("sourceType", out var sourceType) ? sourceType : KnowledgeSourceType.Document;

    private async Task<GeminiRest.GeminiAnswerJson?> CallAndParseAsync(
        GeminiCredentials creds, string prompt, byte[]? audio = null, string? mimeType = null)
    {
        var text = await GeminiRest.CallAsync(httpClientFactory, creds, logger, prompt, audio, mimeType);
        return ParseAnswerJson(text);
    }

    /// <summary>Runs the answer step (3 only) on OpenAI chat-completions. Transcription (1) stays on
    /// Gemini in the caller - this is text-only, reusing the exact same prompt + JSON schema.</summary>
    private async Task<GeminiRest.GeminiAnswerJson?> AnswerWithOpenAiAsync(string transcript, string documentBlock, string qnaBlock)
    {
        var openAi = ExternalServiceEnv.GetOpenAi();
        var text = await OpenAiRest.CallAnswerAsync(
            httpClientFactory, openAi, logger,
            "คุณตอบโดยอ้างอิงจากข้อมูลอ้างอิงที่ให้มาเท่านั้น และตอบกลับเป็น JSON ตาม schema ที่ระบุเท่านั้น",
            BuildAnswerPrompt(transcript, documentBlock, qnaBlock));
        return ParseAnswerJson(text);
    }

    private static GeminiRest.GeminiAnswerJson? ParseAnswerJson(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        // Some models (GLM via ModelArts, seen live) wrap the answer in a ```json ... ``` fence even
        // in json_object mode. Extract the outermost { ... } so the fence (or any stray prose) doesn't
        // break deserialization - a plain JSON body is unaffected.
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<GeminiRest.GeminiAnswerJson>(text[start..(end + 1)], new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Enums;

namespace SupportRoom.Providers.VoiceQuestion;

/// <summary>
/// Real Gemini integration - plain REST call (no SDK), mirrors src/providers/voice-question/
/// gemini-voice-question-provider.ts's prompts and response contract exactly. Sends the whole
/// deck's speaker notes on every question; see RagVoiceQuestionProvider for the
/// retrieval-grounded alternative behind the same IVoiceQuestionProvider contract.
/// </summary>
public sealed class GeminiVoiceQuestionProvider(IHttpClientFactory httpClientFactory, ILogger<GeminiVoiceQuestionProvider> logger) : IVoiceQuestionProvider
{
    private static string BuildPrompt(string groundingContext) => string.Join('\n',
    [
        "คุณคือผู้ช่วยตอบคำถามคุณครูระหว่างบทเรียนสาธิตการใช้งานระบบผ่านเสียง (Push-to-Talk)",
        "ฟังไฟล์เสียงที่แนบมา ถอดเสียงเป็นข้อความภาษาไทย แล้วตอบคำถามโดยอ้างอิงเฉพาะข้อมูลใน Speaker Notes ด้านล่างเท่านั้น",
        "ห้ามตอบจากความรู้ทั่วไป ห้ามเดาคำตอบ ตอบสั้นและกระชับ",
        "",
        "Speaker Notes ทุก Slide ในบทเรียนนี้:",
        groundingContext,
        "",
        "ตอบกลับเป็น JSON เท่านั้น ตาม schema:",
        """{"transcript": string, "answer": string, "answerStatus": "answered" | "not_found" | "out_of_scope" | "transcription_failed", "relatedSlideObjectId": string | null}""",
        "",
        "relatedSlideObjectId: เมื่อ answerStatus = answered ให้ใส่ objectId ของ slide ที่ใช้เป็นแหล่งอ้างอิงคำตอบเสมอ",
        "ต้องเป็น objectId ที่ปรากฏในรายการ Speaker Notes ด้านบนเท่านั้น ห้ามสร้างขึ้นเอง",
        "ถ้าตอบไม่ได้หรือไม่มี slide ไหนเกี่ยวข้อง ให้เป็น null",
    ]);

    // No speaker-notes grounding here on purpose: this is a yes/no reply to "พร้อมหรือยังคะ?",
    // and leaving the whole deck out of the request makes it far quicker than a real question.
    private static string BuildReadinessPrompt() => string.Join('\n',
    [
        "ฟังไฟล์เสียงที่แนบมา คุณครูกำลังตอบคำถามว่า พร้อมเริ่มเรียนหรือยัง",
        "ถอดเสียงเป็นข้อความภาษาไทย แล้วตัดสินว่าคำตอบคือพร้อมหรือยังไม่พร้อม",
        """ถ้าฟังไม่ชัดหรือไม่แน่ใจ ให้ถือว่า "not_ready" เพื่อไม่ให้เริ่มเรียนโดยที่คุณครูยังไม่ได้ตั้งใจ""",
        "",
        "ตอบกลับเป็น JSON เท่านั้น ตาม schema:",
        """{"transcript": string, "readiness": "ready" | "not_ready"}""",
    ]);

    public async Task<VoiceQuestionResult> TranscribeAndAnswerAsync(VoiceQuestionInput input)
    {
        if (input.DurationMs < UploadLimits.MinVoiceDurationMs)
        {
            return new VoiceQuestionResult { AnswerStatus = AnswerStatus.NoSpeech };
        }

        var creds = ExternalServiceEnv.GetGemini();
        var isReadiness = input.Expecting == "readiness";
        var groundingContext = isReadiness
            ? ""
            : string.Join('\n', input.LessonSlides.Select((slide, index) => $"Slide {index + 1} ({slide.SlideObjectId}): {slide.SpeakerNotes}"));

        var text = await GeminiRest.CallAsync(
            httpClientFactory, creds, logger, isReadiness ? BuildReadinessPrompt() : BuildPrompt(groundingContext), input.Audio, input.MimeType);

        if (string.IsNullOrEmpty(text))
        {
            return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<GeminiRest.GeminiAnswerJson>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed is null)
            {
                return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
            }

            if (isReadiness)
            {
                // Anything other than an explicit "ready" is treated as not ready, so a misheard
                // reply stalls the lesson rather than starting one the teacher didn't ask for.
                return new VoiceQuestionResult
                {
                    Transcript = parsed.Transcript ?? "",
                    AnswerStatus = AnswerStatus.Answered,
                    Readiness = parsed.Readiness == "ready" ? "ready" : "not_ready",
                };
            }

            if (!GeminiRest.IsAnswerStatus(parsed.AnswerStatus))
            {
                return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
            }

            return new VoiceQuestionResult
            {
                Transcript = parsed.Transcript ?? "",
                Answer = parsed.Answer ?? "",
                AnswerStatus = parsed.AnswerStatus!,
                RelatedSlideObjectId = parsed.RelatedSlideObjectId,
            };
        }
        catch (JsonException)
        {
            return new VoiceQuestionResult { AnswerStatus = AnswerStatus.TranscriptionFailed };
        }
    }
}

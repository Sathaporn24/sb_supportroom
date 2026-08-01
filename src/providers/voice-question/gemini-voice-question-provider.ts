import "server-only";
import type { VoiceQuestionInput, VoiceQuestionProvider, VoiceQuestionResult } from "@/providers/voice-question/types";
import { getGeminiEnv, uploadLimits } from "@/config/env";
import type { AnswerStatus } from "@/types/domain";

// PREPARED — CREDENTIALS REQUIRED. Not tested against the real Gemini API yet.
// See docs/GEMINI_INTEGRATION.md for the prompt/response schema and safety rules.
const ANSWER_STATUSES: AnswerStatus[] = ["answered", "not_found", "out_of_scope", "no_speech", "transcription_failed"];

function isAnswerStatus(value: unknown): value is AnswerStatus {
  return typeof value === "string" && (ANSWER_STATUSES as string[]).includes(value);
}

function buildPrompt(groundingContext: string): string {
  return [
    "คุณคือผู้ช่วยตอบคำถามคุณครูระหว่างบทเรียนสาธิตการใช้งานระบบผ่านเสียง (Push-to-Talk)",
    "ฟังไฟล์เสียงที่แนบมา ถอดเสียงเป็นข้อความภาษาไทย แล้วตอบคำถามโดยอ้างอิงเฉพาะข้อมูลใน Speaker Notes ด้านล่างเท่านั้น",
    "ห้ามตอบจากความรู้ทั่วไป ห้ามเดาคำตอบ ตอบสั้นและกระชับ",
    "",
    "Speaker Notes ทุก Slide ในบทเรียนนี้:",
    groundingContext,
    "",
    "ตอบกลับเป็น JSON เท่านั้น ตาม schema:",
    '{"transcript": string, "answer": string, "answerStatus": "answered" | "not_found" | "out_of_scope" | "transcription_failed", "relatedSlideObjectId": string | null}',
    "",
    // The room jumps the Slides embed to relatedSlideObjectId while the answer is read out
    // loud, so an omitted or invented id leaves the teacher staring at the wrong slide.
    "relatedSlideObjectId: เมื่อ answerStatus = answered ให้ใส่ objectId ของ slide ที่ใช้เป็นแหล่งอ้างอิงคำตอบเสมอ",
    "ต้องเป็น objectId ที่ปรากฏในรายการ Speaker Notes ด้านบนเท่านั้น ห้ามสร้างขึ้นเอง",
    "ถ้าตอบไม่ได้หรือไม่มี slide ไหนเกี่ยวข้อง ให้เป็น null",
  ].join("\n");
}

export class GeminiVoiceQuestionProvider implements VoiceQuestionProvider {
  async transcribeAndAnswer(input: VoiceQuestionInput): Promise<VoiceQuestionResult> {
    if (input.durationMs < uploadLimits.minVoiceDurationMs) {
      return { transcript: "", answer: "", answerStatus: "no_speech" };
    }

    const { apiKey, model } = getGeminiEnv();
    const groundingContext = input.lessonSlides
      .map((slide, index) => `Slide ${index + 1} (${slide.slideObjectId}): ${slide.speakerNotes}`)
      .join("\n");

    const response = await fetch(
      `https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${apiKey}`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          contents: [
            {
              parts: [
                { text: buildPrompt(groundingContext) },
                { inline_data: { mime_type: input.mimeType, data: input.audio.toString("base64") } },
              ],
            },
          ],
          generationConfig: { responseMimeType: "application/json" },
        }),
      },
    );

    if (!response.ok) {
      const errorText = await response.text().catch(() => "");
      throw new Error(`Gemini request failed (${response.status}): ${errorText.slice(0, 200)}`);
    }

    const data = (await response.json()) as {
      candidates?: Array<{ content?: { parts?: Array<{ text?: string }> } }>;
    };
    const text = data.candidates?.[0]?.content?.parts?.[0]?.text;
    if (!text) {
      return { transcript: "", answer: "", answerStatus: "transcription_failed" };
    }

    try {
      const parsed = JSON.parse(text) as Partial<VoiceQuestionResult>;
      if (!isAnswerStatus(parsed.answerStatus)) {
        return { transcript: "", answer: "", answerStatus: "transcription_failed" };
      }
      return {
        transcript: parsed.transcript ?? "",
        answer: parsed.answer ?? "",
        answerStatus: parsed.answerStatus,
        relatedSlideObjectId: parsed.relatedSlideObjectId ?? undefined,
      };
    } catch {
      return { transcript: "", answer: "", answerStatus: "transcription_failed" };
    }
  }
}

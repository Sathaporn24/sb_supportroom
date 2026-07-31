import type { VoiceQuestionInput, VoiceQuestionProvider, VoiceQuestionResult } from "@/providers/voice-question/types";
import { UNKNOWN_ANSWER_TEXT } from "@/config/response-texts";
import { uploadLimits } from "@/config/env";

// Cannot really transcribe audio without a real STT backend, so this always "hears"
// the same demo question once a long-enough recording comes in, then grounds the
// answer against the real lessonSlides notes passed in - proving the full
// record → upload → ground → answer pipeline without a Gemini key.
const MOCK_TRANSCRIPT = "เข้าสู่ระบบไม่สำเร็จต้องทำอย่างไร";
const MOCK_KEYWORDS = ["ไม่สำเร็จ", "ผู้ดูแลระบบ"];

export class MockVoiceQuestionProvider implements VoiceQuestionProvider {
  async transcribeAndAnswer(input: VoiceQuestionInput): Promise<VoiceQuestionResult> {
    if (input.durationMs < uploadLimits.minVoiceDurationMs) {
      return { transcript: "", answer: "", answerStatus: "no_speech" };
    }

    const transcript = MOCK_TRANSCRIPT;
    const match = input.lessonSlides.find((slide) =>
      MOCK_KEYWORDS.some((keyword) => slide.speakerNotes.includes(keyword)),
    );

    if (!match) {
      return { transcript, answer: UNKNOWN_ANSWER_TEXT, answerStatus: "not_found" };
    }

    return {
      transcript,
      answer: match.speakerNotes,
      answerStatus: "answered",
      relatedSlideObjectId: match.slideObjectId,
    };
  }
}

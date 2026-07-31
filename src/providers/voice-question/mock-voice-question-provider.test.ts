import { describe, expect, it } from "vitest";
import { MockVoiceQuestionProvider } from "@/providers/voice-question/mock-voice-question-provider";

const provider = new MockVoiceQuestionProvider();
const baseInput = { audio: Buffer.from([]), mimeType: "audio/webm", lessonSlides: [] };

describe("MockVoiceQuestionProvider", () => {
  it("returns no_speech for recordings shorter than MIN_VOICE_DURATION_MS", async () => {
    const result = await provider.transcribeAndAnswer({ ...baseInput, durationMs: 100 });
    expect(result.answerStatus).toBe("no_speech");
    expect(result.transcript).toBe("");
  });

  it("returns not_found when no slide notes match the demo transcript's keywords", async () => {
    const result = await provider.transcribeAndAnswer({
      ...baseInput,
      durationMs: 1000,
      lessonSlides: [{ slideObjectId: "s1", speakerNotes: "เนื้อหาที่ไม่เกี่ยวข้องเลย" }],
    });
    expect(result.answerStatus).toBe("not_found");
  });

  it("grounds the answer in the matching slide's speaker notes", async () => {
    const result = await provider.transcribeAndAnswer({
      ...baseInput,
      durationMs: 1000,
      lessonSlides: [
        { slideObjectId: "s1", speakerNotes: "ไม่เกี่ยวข้อง" },
        { slideObjectId: "s2", speakerNotes: "หากเข้าสู่ระบบไม่สำเร็จ ให้ติดต่อผู้ดูแลระบบ" },
      ],
    });
    expect(result.answerStatus).toBe("answered");
    expect(result.relatedSlideObjectId).toBe("s2");
    expect(result.answer).toContain("ผู้ดูแลระบบ");
  });
});

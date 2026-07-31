import { afterEach, describe, expect, it, vi } from "vitest";

describe("provider factories default to Mock when no provider env var is set", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.resetModules();
  });

  it("createSlidesContentProvider returns MockSlidesContentProvider", async () => {
    vi.stubEnv("SLIDES_PROVIDER", "");
    const { createSlidesContentProvider } = await import("@/providers/slides");
    const { MockSlidesContentProvider } = await import("@/providers/slides/mock-slides-provider");
    expect(createSlidesContentProvider()).toBeInstanceOf(MockSlidesContentProvider);
  });

  it("createTextToSpeechProvider returns MockTextToSpeechProvider", async () => {
    vi.stubEnv("TTS_PROVIDER", "");
    const { createTextToSpeechProvider } = await import("@/providers/tts");
    const { MockTextToSpeechProvider } = await import("@/providers/tts/mock-tts-provider");
    expect(createTextToSpeechProvider()).toBeInstanceOf(MockTextToSpeechProvider);
  });

  it("createVoiceQuestionProvider returns MockVoiceQuestionProvider", async () => {
    vi.stubEnv("VOICE_QUESTION_PROVIDER", "");
    const { createVoiceQuestionProvider } = await import("@/providers/voice-question");
    const { MockVoiceQuestionProvider } = await import("@/providers/voice-question/mock-voice-question-provider");
    expect(createVoiceQuestionProvider()).toBeInstanceOf(MockVoiceQuestionProvider);
  });

  it("createLessonConfigRepository / createSessionRepository return Mock repositories", async () => {
    vi.stubEnv("DATA_PROVIDER", "");
    const { createLessonConfigRepository, createSessionRepository } = await import("@/providers/data");
    const { MockLessonConfigRepository } = await import("@/providers/data/mock/mock-lesson-config-repository");
    const { MockSessionRepository } = await import("@/providers/data/mock/mock-session-repository");
    expect(createLessonConfigRepository()).toBeInstanceOf(MockLessonConfigRepository);
    expect(createSessionRepository()).toBeInstanceOf(MockSessionRepository);
  });
});

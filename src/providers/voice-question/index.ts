import "server-only";
import { MockVoiceQuestionProvider } from "@/providers/voice-question/mock-voice-question-provider";
import { GeminiVoiceQuestionProvider } from "@/providers/voice-question/gemini-voice-question-provider";
import type { VoiceQuestionProvider } from "@/providers/voice-question/types";
import { getProviderSelection } from "@/config/env";

export function createVoiceQuestionProvider(): VoiceQuestionProvider {
  const { VOICE_QUESTION_PROVIDER } = getProviderSelection();
  switch (VOICE_QUESTION_PROVIDER) {
    case "gemini":
      return new GeminiVoiceQuestionProvider();
    case "mock":
    default:
      return new MockVoiceQuestionProvider();
  }
}

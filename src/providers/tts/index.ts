import "server-only";
import { MockTextToSpeechProvider } from "@/providers/tts/mock-tts-provider";
import { HuggingFaceTextToSpeechProvider } from "@/providers/tts/huggingface-tts-provider";
import type { TextToSpeechProvider } from "@/providers/tts/types";
import { getProviderSelection } from "@/config/env";

export function createTextToSpeechProvider(): TextToSpeechProvider {
  const { TTS_PROVIDER } = getProviderSelection();
  switch (TTS_PROVIDER) {
    case "huggingface":
      return new HuggingFaceTextToSpeechProvider();
    case "mock":
    default:
      return new MockTextToSpeechProvider();
  }
}

import "server-only";
import { MockTextToSpeechProvider } from "@/providers/tts/mock-tts-provider";
import { EdgeTextToSpeechProvider } from "@/providers/tts/edge-tts-provider";
import type { TextToSpeechProvider } from "@/providers/tts/types";
import { getProviderSelection } from "@/config/env";

export function createTextToSpeechProvider(): TextToSpeechProvider {
  const { TTS_PROVIDER } = getProviderSelection();
  switch (TTS_PROVIDER) {
    case "edge":
      return new EdgeTextToSpeechProvider();
    case "mock":
    default:
      return new MockTextToSpeechProvider();
  }
}

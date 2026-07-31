import { MockTextToSpeechProvider } from "@/providers/tts/mock-tts-provider";
import { BrowserTextToSpeechProvider } from "@/providers/tts/browser-tts-provider";
import { ExternalTextToSpeechProvider } from "@/providers/tts/external-tts-provider";
import type { TextToSpeechProvider } from "@/providers/tts/types";
import { providerConfig } from "@/config/tutor-config";

function createTextToSpeechProvider(): TextToSpeechProvider {
  switch (providerConfig.ttsProvider) {
    case "browser":
      return new BrowserTextToSpeechProvider();
    case "external":
      return new ExternalTextToSpeechProvider();
    case "mock":
    default:
      return new MockTextToSpeechProvider();
  }
}

export const textToSpeechProvider: TextToSpeechProvider = createTextToSpeechProvider();

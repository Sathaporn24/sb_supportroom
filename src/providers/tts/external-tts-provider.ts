import type { SpeakOptions, TextToSpeechProvider } from "@/providers/tts/types";

// TODO(Phase 2): implement a real hosted TTS provider (e.g. Gemini Live voice or another
// vendor). No API key belongs here - calls should go through a backend once one exists.
export class ExternalTextToSpeechProvider implements TextToSpeechProvider {
  async speak(_text: string, _options?: SpeakOptions): Promise<void> {
    throw new Error("ExternalTextToSpeechProvider is not implemented in the mock phase.");
  }

  stop(): void {
    // no-op placeholder
  }
}

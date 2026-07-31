import type { SpeechToTextProvider } from "@/providers/stt/types";

// TODO(Phase 2): implement Thai speech-to-text via a server-side provider once
// microphone audio needs to be transcribed for real. No API key belongs here.
export class ServerSpeechToTextProvider implements SpeechToTextProvider {
  async start(_onText: (text: string) => void): Promise<void> {
    throw new Error("ServerSpeechToTextProvider is not implemented in the mock phase.");
  }

  async stop(): Promise<void> {
    // no-op placeholder
  }
}

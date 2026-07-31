import type { SpeechToTextProvider } from "@/providers/stt/types";

// Does not listen to a real microphone. Demo Controls and the chat drawer call
// pushTranscript() to simulate recognized speech, which forwards to the onText
// callback registered via start() - matching the SpeechToTextProvider contract.
export class MockSpeechToTextProvider implements SpeechToTextProvider {
  private onTextCallback: ((text: string) => void) | null = null;

  async start(onText: (text: string) => void): Promise<void> {
    this.onTextCallback = onText;
  }

  async stop(): Promise<void> {
    this.onTextCallback = null;
  }

  pushTranscript(text: string): void {
    this.onTextCallback?.(text);
  }
}

import type { SpeakOptions, TextToSpeechProvider } from "@/providers/tts/types";
import { MockTextToSpeechProvider } from "@/providers/tts/mock-tts-provider";

// Optional Web Speech API provider. Disabled by default (NEXT_PUBLIC_TTS_PROVIDER=mock).
// Falls back to the mock timer whenever speechSynthesis is unavailable so the Tutor
// Engine never has to know which provider is active.
export class BrowserTextToSpeechProvider implements TextToSpeechProvider {
  private fallback = new MockTextToSpeechProvider();
  private utterance: SpeechSynthesisUtterance | null = null;

  private isSupported(): boolean {
    return typeof window !== "undefined" && "speechSynthesis" in window;
  }

  async speak(text: string, options?: SpeakOptions): Promise<void> {
    if (!this.isSupported()) {
      return this.fallback.speak(text, options);
    }

    this.stop();

    return new Promise<void>((resolve, reject) => {
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = "th-TH";
      this.utterance = utterance;

      const onAbort = () => {
        window.speechSynthesis.cancel();
        reject(new DOMException("aborted", "AbortError"));
      };

      if (options?.signal) {
        if (options.signal.aborted) {
          onAbort();
          return;
        }
        options.signal.addEventListener("abort", onAbort, { once: true });
      }

      utterance.onend = () => {
        options?.signal?.removeEventListener("abort", onAbort);
        resolve();
      };
      utterance.onerror = () => {
        options?.signal?.removeEventListener("abort", onAbort);
        // Fall back to the mock timer rather than failing the whole flow.
        this.fallback.speak(text, options).then(resolve, reject);
      };

      window.speechSynthesis.speak(utterance);
    });
  }

  stop(): void {
    this.fallback.stop();
    if (this.isSupported()) {
      window.speechSynthesis.cancel();
    }
    this.utterance = null;
  }
}

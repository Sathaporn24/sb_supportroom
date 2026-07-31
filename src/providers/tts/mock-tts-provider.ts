import type { SpeakOptions, TextToSpeechProvider } from "@/providers/tts/types";
import { estimateSpeakingDurationMs } from "@/utils/text";
import { tutorConfig } from "@/config/tutor-config";

// No real audio is produced - this only runs a timer sized to the text length
// (or an explicit Segment.mockSpeakDurationMs) and resolves when "speaking" ends.
export class MockTextToSpeechProvider implements TextToSpeechProvider {
  private activeTimer: ReturnType<typeof setTimeout> | null = null;
  private activeReject: ((reason?: unknown) => void) | null = null;

  async speak(text: string, options?: SpeakOptions): Promise<void> {
    this.stop();
    const durationMs = options?.durationMs ?? estimateSpeakingDurationMs(text, tutorConfig.mockWordsPerMinute);

    return new Promise<void>((resolve, reject) => {
      this.activeReject = reject;

      const onAbort = () => {
        this.clear();
        reject(new DOMException("aborted", "AbortError"));
      };

      if (options?.signal) {
        if (options.signal.aborted) {
          onAbort();
          return;
        }
        options.signal.addEventListener("abort", onAbort, { once: true });
      }

      this.activeTimer = setTimeout(() => {
        options?.signal?.removeEventListener("abort", onAbort);
        this.clear();
        resolve();
      }, durationMs);
    });
  }

  stop(): void {
    if (this.activeReject) {
      const reject = this.activeReject;
      this.clear();
      reject(new DOMException("aborted", "AbortError"));
    }
  }

  private clear(): void {
    if (this.activeTimer) {
      clearTimeout(this.activeTimer);
    }
    this.activeTimer = null;
    this.activeReject = null;
  }
}

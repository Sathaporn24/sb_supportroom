export type SpeakOptions = {
  signal?: AbortSignal;
  /** Optional explicit duration override (e.g. a Segment's mockSpeakDurationMs). */
  durationMs?: number;
};

export interface TextToSpeechProvider {
  speak(text: string, options?: SpeakOptions): Promise<void>;
  stop(): void;
}

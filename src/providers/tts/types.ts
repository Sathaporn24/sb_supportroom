export type TtsInput = {
  text: string;
  voice?: string;
  /**
   * SSML rate override (e.g. "-45%") for utterances that shouldn't run at lesson pace.
   * Repeating characters barely stretches Thai output - measured, "อืมมมมมม" buys only
   * ~0.2s over "อืม" - so drawn-out thinking sounds are made by slowing the rate instead.
   * Falls back to EDGE_TTS_RATE when omitted.
   */
  rate?: string;
};

export type TtsResult = {
  audio: Buffer;
  mimeType: string;
};

export interface TextToSpeechProvider {
  synthesize(input: TtsInput): Promise<TtsResult>;
}

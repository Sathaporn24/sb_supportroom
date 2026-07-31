export type TtsInput = {
  text: string;
  voice?: string;
};

export type TtsResult = {
  audio: Buffer;
  mimeType: string;
};

export interface TextToSpeechProvider {
  synthesize(input: TtsInput): Promise<TtsResult>;
}

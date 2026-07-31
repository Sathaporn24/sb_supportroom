export interface SpeechToTextProvider {
  start(onText: (text: string) => void): Promise<void>;
  stop(): Promise<void>;
}

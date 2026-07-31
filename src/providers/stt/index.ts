import { MockSpeechToTextProvider } from "@/providers/stt/mock-stt-provider";

// The mock provider is the only one wired up for UI consumption in this phase - it
// exposes pushTranscript() so Chat / Demo Controls can simulate recognized speech.
// ServerSpeechToTextProvider (see server-stt-provider.ts) implements the same
// SpeechToTextProvider interface and is the Phase 2 swap-in once real STT is ready.
export const speechToTextProvider = new MockSpeechToTextProvider();

import type { TextToSpeechProvider, TtsInput, TtsResult } from "@/providers/tts/types";
import { estimateSpeakingDurationMs } from "@/utils/text";
import { tutorConfig } from "@/config/tutor-config";

const SAMPLE_RATE = 8_000;

/**
 * Builds a real (silent) mono 16-bit PCM WAV file sized to the estimated speaking
 * duration. The browser's <audio> element can play/pause/seek it and fires a genuine
 * "ended" event, so the client-side playback code path is identical to the real
 * Hugging Face provider - only the audio content differs.
 */
function buildSilentWav(durationMs: number): Buffer {
  const numSamples = Math.max(1, Math.round((durationMs / 1000) * SAMPLE_RATE));
  const dataSize = numSamples * 2;
  const buffer = Buffer.alloc(44 + dataSize);

  buffer.write("RIFF", 0, "ascii");
  buffer.writeUInt32LE(36 + dataSize, 4);
  buffer.write("WAVE", 8, "ascii");
  buffer.write("fmt ", 12, "ascii");
  buffer.writeUInt32LE(16, 16);
  buffer.writeUInt16LE(1, 20); // PCM
  buffer.writeUInt16LE(1, 22); // mono
  buffer.writeUInt32LE(SAMPLE_RATE, 24);
  buffer.writeUInt32LE(SAMPLE_RATE * 2, 28); // byte rate
  buffer.writeUInt16LE(2, 32); // block align
  buffer.writeUInt16LE(16, 34); // bits per sample
  buffer.write("data", 36, "ascii");
  buffer.writeUInt32LE(dataSize, 40);
  // PCM sample bytes are already zero (silence) from Buffer.alloc.

  return buffer;
}

export class MockTextToSpeechProvider implements TextToSpeechProvider {
  async synthesize(input: TtsInput): Promise<TtsResult> {
    const durationMs = estimateSpeakingDurationMs(input.text, tutorConfig.mockWordsPerMinute);
    return { audio: buildSilentWav(durationMs), mimeType: "audio/wav" };
  }
}

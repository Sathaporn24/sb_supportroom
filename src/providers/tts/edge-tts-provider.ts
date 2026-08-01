import "server-only";
import { MsEdgeTTS, OUTPUT_FORMAT } from "msedge-tts";
import type { TextToSpeechProvider, TtsInput, TtsResult } from "@/providers/tts/types";

const DEFAULT_VOICE = process.env.EDGE_TTS_VOICE || "th-TH-PremwadeeNeural";
// -10% verified to sound more natural for instructional narration than the raw default rate.
const DEFAULT_RATE = process.env.EDGE_TTS_RATE || "-10%";

// CONNECTED - verified against the real Microsoft Edge Read Aloud service (no API key
// needed). Replaces the Hugging Face TTS provider: facebook/mms-tts-tha has no inference
// provider hosting it anymore (checked via huggingface.co/api/models - inferenceProviderMapping
// is empty), and the free serverless TTS models that remain (Kokoro, Chatterbox, Orpheus)
// are English-only. Edge TTS has full-quality Thai neural voices and works today.
export class EdgeTextToSpeechProvider implements TextToSpeechProvider {
  async synthesize(input: TtsInput): Promise<TtsResult> {
    const tts = new MsEdgeTTS();
    try {
      await tts.setMetadata(input.voice || DEFAULT_VOICE, OUTPUT_FORMAT.AUDIO_24KHZ_48KBITRATE_MONO_MP3);
      const { audioStream } = tts.toStream(input.text, { rate: input.rate || DEFAULT_RATE });

      const chunks: Buffer[] = [];
      for await (const chunk of audioStream) {
        chunks.push(chunk as Buffer);
      }
      return { audio: Buffer.concat(chunks), mimeType: "audio/mpeg" };
    } finally {
      tts.close();
    }
  }
}

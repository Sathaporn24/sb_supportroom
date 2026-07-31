import "server-only";
import type { TextToSpeechProvider, TtsInput, TtsResult } from "@/providers/tts/types";
import { getHuggingFaceEnv } from "@/config/env";

// PREPARED — CREDENTIALS REQUIRED. Not tested against a real Hugging Face model yet.
// See docs/HUGGINGFACE_TTS_SETUP.md for how to pick and evaluate a Thai TTS model,
// and for cold-start/rate-limit behavior to expect from the Inference API.
export class HuggingFaceTextToSpeechProvider implements TextToSpeechProvider {
  async synthesize(input: TtsInput): Promise<TtsResult> {
    const { token, endpoint } = getHuggingFaceEnv();

    const response = await fetch(endpoint, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ inputs: input.text }),
    });

    if (!response.ok) {
      const errorText = await response.text().catch(() => "");
      throw new Error(`Hugging Face TTS request failed (${response.status}): ${errorText.slice(0, 200)}`);
    }

    const mimeType = response.headers.get("content-type") ?? "audio/flac";
    const arrayBuffer = await response.arrayBuffer();
    return { audio: Buffer.from(arrayBuffer), mimeType };
  }
}

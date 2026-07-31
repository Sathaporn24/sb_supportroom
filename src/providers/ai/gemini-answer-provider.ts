import type { AiAnswerProvider, AnswerContext, AnswerResult } from "@/providers/ai/types";

// TODO(Phase 3): wire up Gemini for intent classification + grounded Q&A over the
// lesson script and FAQ. Must keep answers grounded (no general knowledge fallback)
// and continue returning AnswerResult so the Tutor Engine does not need to change.
// Do not add an API key here - keys belong server-side once a backend exists.
export class GeminiAnswerProvider implements AiAnswerProvider {
  async answer(_context: AnswerContext): Promise<AnswerResult> {
    throw new Error("GeminiAnswerProvider is not implemented in the mock phase.");
  }
}

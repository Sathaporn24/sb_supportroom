import { MockAiAnswerProvider } from "@/providers/ai/mock-ai-answer-provider";
import { GeminiAnswerProvider } from "@/providers/ai/gemini-answer-provider";
import type { AiAnswerProvider } from "@/providers/ai/types";
import { providerConfig } from "@/config/tutor-config";

function createAiAnswerProvider(): AiAnswerProvider {
  switch (providerConfig.aiProvider) {
    case "gemini":
      return new GeminiAnswerProvider();
    case "mock":
    default:
      return new MockAiAnswerProvider();
  }
}

export const aiAnswerProvider: AiAnswerProvider = createAiAnswerProvider();

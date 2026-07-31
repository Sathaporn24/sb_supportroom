import type { AiAnswerProvider, AnswerContext, AnswerResult } from "@/providers/ai/types";
import { OUT_OF_SCOPE_TEXT, UNKNOWN_ANSWER_TEXT } from "@/config/response-texts";

// Matches the question against the active FAQ list for the lesson snapshot only.
// Never falls back to general knowledge - grounded answers are a hard product rule.
export class MockAiAnswerProvider implements AiAnswerProvider {
  async answer(context: AnswerContext): Promise<AnswerResult> {
    const normalized = context.question.trim().toLowerCase();

    if (!normalized) {
      return { text: UNKNOWN_ANSWER_TEXT, scope: "UNKNOWN", shouldFlagForCs: true };
    }

    const activeFaqs = context.lessonSnapshot.faqs.filter((faq) => faq.active);
    const match = activeFaqs.find((faq) => {
      if (normalized.includes(faq.question.toLowerCase())) {
        return true;
      }
      return faq.keywords.some((keyword) => normalized.includes(keyword.toLowerCase()));
    });

    if (!match) {
      return { text: UNKNOWN_ANSWER_TEXT, scope: "UNKNOWN", shouldFlagForCs: true };
    }

    if (match.scope === "OUT_OF_SCOPE") {
      return { text: match.answer || OUT_OF_SCOPE_TEXT, scope: "OUT_OF_SCOPE", shouldFlagForCs: false };
    }

    return {
      text: match.answer,
      scope: match.scope,
      relatedMediaId: match.relatedMediaId,
      shouldFlagForCs: false,
    };
  }
}

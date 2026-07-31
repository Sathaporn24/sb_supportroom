import type { Lesson, QuestionScope } from "@/types/domain";

export type AnswerContext = {
  lessonSnapshot: Lesson;
  currentStepIndex: number;
  currentSegmentIndex: number;
  question: string;
};

export type AnswerResult = {
  text: string;
  scope: QuestionScope;
  relatedMediaId?: string;
  shouldFlagForCs: boolean;
};

export interface AiAnswerProvider {
  answer(context: AnswerContext): Promise<AnswerResult>;
}

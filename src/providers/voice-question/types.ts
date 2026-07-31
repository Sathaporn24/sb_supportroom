import type { AnswerStatus } from "@/types/domain";

export type VoiceQuestionSlideContext = {
  slideObjectId: string;
  speakerNotes: string;
};

export type VoiceQuestionInput = {
  audio: Buffer;
  mimeType: string;
  /** Client-measured hold duration, used for the no_speech / too-short check. */
  durationMs: number;
  /** Every slide's speaker notes in the lesson - the full grounding knowledge base. */
  lessonSlides: VoiceQuestionSlideContext[];
  currentSlideObjectId?: string;
};

export type VoiceQuestionResult = {
  transcript: string;
  answer: string;
  answerStatus: AnswerStatus;
  relatedSlideObjectId?: string;
};

export interface VoiceQuestionProvider {
  transcribeAndAnswer(input: VoiceQuestionInput): Promise<VoiceQuestionResult>;
}

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
  /**
   * "readiness" is the reply to the "พร้อมหรือยังคะ?" prompt - a yes/no, not a question.
   * It skips the speaker-notes grounding entirely, which also makes it markedly faster
   * than a full question round-trip.
   */
  expecting?: "question" | "readiness";
};

export type VoiceQuestionResult = {
  transcript: string;
  answer: string;
  answerStatus: AnswerStatus;
  relatedSlideObjectId?: string;
  /** Only set when expecting === "readiness": did the teacher say they're ready to start? */
  readiness?: "ready" | "not_ready";
};

export interface VoiceQuestionProvider {
  transcribeAndAnswer(input: VoiceQuestionInput): Promise<VoiceQuestionResult>;
}

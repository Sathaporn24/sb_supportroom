// Google Slides is the source of truth for teaching content in this phase.
// LessonConfig only stores admin-set metadata (URLs, timing, per-slide video duration) -
// the actual slide content (speaker notes, images/video) is resolved live via
// SlidesContentProvider and is never persisted as a copy.

export type SlideConfig = {
  slideObjectId: string;
  slideIndex: number;
  /** null/0 for slides with no video. */
  videoDurationMs: number | null;
};

export type LessonConfig = {
  id: string;
  slug: string;
  title: string;
  description?: string;
  slidesSourceUrl: string;
  /** Extracted from slidesSourceUrl when possible; required for the Google Slides API. */
  presentationId: string | null;
  /** Published/embed URL used to render the Shared Screen iframe. */
  slidesEmbedUrl: string | null;
  introWaitMs: number;
  breathPauseMs: number;
  finalQuestionWaitMs: number;
  slideConfigs: SlideConfig[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

// presentationId is always derived server-side from slidesSourceUrl - CS never sets it directly.
export type LessonConfigInput = Omit<LessonConfig, "id" | "createdAt" | "updatedAt" | "presentationId">;

/** A single resolved slide as returned live by SlidesContentProvider (no admin-only fields). */
export type ResolvedSlide = {
  slideObjectId: string;
  index: number;
  speakerNotes: string;
  slideUrl?: string;
};

export type SlidesLessonContent = {
  presentationId: string;
  title: string;
  embedUrl: string;
  slides: ResolvedSlide[];
  syncedAt: string;
};

/** ResolvedSlide merged with the admin-configured videoDurationMs - what the Tutor Engine consumes. */
export type TeachingSlide = ResolvedSlide & {
  videoDurationMs: number;
};

export type SessionStatus = "NOT_STARTED" | "IN_PROGRESS" | "ENDED" | "EXPIRED";

export type TrainingSession = {
  id: string;
  token: string;
  lessonId: string;
  lessonSlug: string;
  teacherName?: string;
  schoolName?: string;
  status: SessionStatus;
  createdAt: string;
  expiresAt: string;
  startedAt?: string;
  endedAt?: string;
  completedAllSlides: boolean;
  lastSlideObjectId?: string;
};

export type CreateSessionInput = {
  lessonSlug: string;
  teacherName?: string;
  schoolName?: string;
  expiresAt: string;
};

export type EndSessionInput = {
  completedAllSlides: boolean;
  lastSlideObjectId?: string;
};

/**
 * Mirrors the Gemini grounded-answer result types from the spec - never a plain
 * boolean, so the UI/summary can distinguish *why* a question wasn't answered.
 */
export type AnswerStatus = "answered" | "not_found" | "out_of_scope" | "no_speech" | "transcription_failed";

export type SessionQuestion = {
  id: string;
  sessionId: string;
  slideObjectId?: string;
  transcript?: string;
  answer?: string;
  answerStatus: AnswerStatus;
  createdAt: string;
};

export type CreateSessionQuestionInput = Omit<SessionQuestion, "id" | "createdAt">;

export type SessionSummary = {
  sessionId: string;
  completedAllSlides: boolean;
  lastSlideObjectId?: string;
  questions: SessionQuestion[];
  unansweredPoints: string[];
  createdAt: string;
};

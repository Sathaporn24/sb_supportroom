// Teaching content comes from either Google Slides or an uploaded PDF (contentSourceType).
// LessonConfig only stores admin-set metadata (URLs/PDF pointer, timing, per-slide video
// duration) - the actual slide content (speaker notes, images/video) is resolved live via
// SlidesContentProvider/PdfSlidesRenderer and is never persisted as a copy.

export type SlideConfig = {
  slideObjectId: string;
  slideIndex: number;
  /** null/0 for slides with no video. */
  videoDurationMs: number | null;
};

export type ContentSourceType = "google_slides" | "pdf";

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
  contentSourceType: ContentSourceType;
  /** Set only when contentSourceType is "pdf" - the DocumentResource holding the PDF. */
  pdfDocumentResourceId?: string;
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

/** Response shape for POST /api/slides/resolve. */
export type ResolvedPresentation = {
  presentationId: string | null;
  embedUrl: string;
  /** True when presentationId could not be derived and only display is possible. */
  isEmbedOnly: boolean;
  warning?: string;
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

/** Response shape for POST /api/voice-question. */
export type VoiceQuestionResult = {
  transcript: string;
  answer: string;
  answerStatus: AnswerStatus;
  relatedSlideObjectId?: string;
  /** Only set when expecting === "readiness": did the teacher say they're ready to start? */
  readiness?: "ready" | "not_ready";
};

export type SessionSummary = {
  sessionId: string;
  completedAllSlides: boolean;
  lastSlideObjectId?: string;
  questions: SessionQuestion[];
  unansweredPoints: string[];
  createdAt: string;
};

/** A typed chat message - separate from SessionQuestion (Push-to-Talk log), sent live over SignalR. */
export type ChatSenderRole = "teacher" | "cs" | "system";

export type ChatMessage = {
  id: string;
  sessionId: string;
  senderRole: ChatSenderRole;
  senderName?: string;
  text: string;
  createdAt: string;
};

/**
 * A CS-uploaded document (.pptx/.pdf/.docx/.xlsx) parsed and embedded into the knowledge base.
 * lessonId null/undefined = standalone document, queried alongside every lesson's own content
 * (see kb-global namespace) instead of being tied to one lesson.
 */
export type DocumentIndexingStatus = "pending" | "indexed" | "failed";

export type DocumentResource = {
  id: string;
  lessonId?: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  indexingStatus: DocumentIndexingStatus;
  indexedChunkCount: number;
  createdAt: string;
};

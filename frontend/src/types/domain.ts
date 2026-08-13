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

// A link and one person's run through it are two different things (CORE_FEATURE_SPEC §1).
// CS creates a TrainingLink and sends it to a whole department; each person who opens it gets
// their own LearningSession. "Session" means the latter everywhere - SessionQuestion.sessionId
// and ChatMessage.sessionId both point at a LearningSession.

/** Computed from expiresAt server-side, never stored. */
export type LinkStatus = "ACTIVE" | "EXPIRED";

export type TrainingLink = {
  id: string;
  token: string;
  lessonId: string;
  lessonSlug: string;
  /** The receiving organization (a school, a branch, a department) - a display label CS types. */
  recipientOrgName?: string;
  status: LinkStatus;
  createdAt: string;
  expiresAt: string;
  /** null = unlimited. Stored but not enforced yet. */
  maxAttendees?: number;
  /** How many people have opened this link. */
  learningSessionCount: number;
};

export type CreateTrainingLinkInput = {
  lessonSlug: string;
  recipientOrgName?: string;
  expiresAt: string;
  maxAttendees?: number;
};

/** No NOT_STARTED: a row only exists once someone has joined. Expiry belongs to the link. */
export type LearningSessionStatus = "IN_PROGRESS" | "ENDED";

export type LearningSession = {
  id: string;
  trainingLinkId: string;
  /** What the learner typed on the join screen. Not an identity - duplicates are fine. */
  recipientName: string;
  status: LearningSessionStatus;
  startedAt: string;
  endedAt?: string;
  lastActivityAt: string;
  lastSlideObjectId?: string;
  lastSlideIndex: number;
  completedAllSlides: boolean;
  /**
   * Derived server-side from lastActivityAt, never stored: still IN_PROGRESS but nothing has
   * moved for longer than INACTIVE_THRESHOLD_MINUTES. A browser "I'm leaving" signal would miss
   * exactly the cases that matter (closed laptop, dead battery, lost connection).
   */
  isStalled: boolean;
  questionCount: number;
};

/** What the join screen posts. Name only - see CORE_FEATURE_SPEC §5.1 for why nothing else. */
export type JoinLearningSessionInput = {
  recipientName: string;
  learnerKey: string;
};

export type UpdateLearningProgressInput = {
  learnerKey: string;
  lastSlideObjectId?: string;
  lastSlideIndex: number;
};

export type EndLearningSessionInput = {
  learnerKey: string;
  completedAllSlides: boolean;
  lastSlideObjectId?: string;
  lastSlideIndex: number;
};

/**
 * Mirrors the Gemini grounded-answer result types from the spec - never a plain
 * boolean, so the UI/summary can distinguish *why* a question wasn't answered.
 */
export type AnswerStatus = "answered" | "not_found" | "out_of_scope" | "no_speech" | "transcription_failed";

/** CS's verdict on one AI answer. The free-text note lives alongside it - "incorrect" alone can't
 * tell a missing document from a retrieval miss from the model inventing an answer, and those are
 * fixed in three different places (CORE_FEATURE_SPEC §2.7). */
export type ReviewResult = "correct" | "incorrect";

export type SessionQuestion = {
  id: string;
  /** A LearningSession id - the question belongs to the person who asked it. */
  sessionId: string;
  slideObjectId?: string;
  transcript?: string;
  answer?: string;
  answerStatus: AnswerStatus;
  createdAt: string;
  /** CS-facing only. The learner's own recap never renders these. */
  reviewResult?: ReviewResult;
  reviewNote?: string;
  reviewedAt?: string;
};

export type CreateSessionQuestionInput = Omit<
  SessionQuestion,
  "id" | "createdAt" | "reviewResult" | "reviewNote" | "reviewedAt"
>;

export type ReviewSessionQuestionInput = {
  reviewResult: ReviewResult;
  reviewNote?: string;
};

/** Response shape for POST /api/voice-question. */
export type VoiceQuestionResult = {
  transcript: string;
  answer: string;
  answerStatus: AnswerStatus;
  relatedSlideObjectId?: string;
  /** Only set when expecting === "readiness": did the teacher say they're ready to start? */
  readiness?: "ready" | "not_ready";
};

/**
 * Computed server-side on every read - there is no summary table (TD-013). Shape unchanged from
 * when it was table-backed.
 */
export type SessionSummary = {
  /** A LearningSession id. */
  sessionId: string;
  completedAllSlides: boolean;
  lastSlideObjectId?: string;
  questions: SessionQuestion[];
  /** ⚠️ Internal - what CS follows up on. Never render this on the learner's own recap
   * (CORE_FEATURE_SPEC §2.5). */
  unansweredPoints: string[];
  createdAt: string;
};

/** A typed chat message - separate from SessionQuestion (Push-to-Talk log), sent live over SignalR. */
// "recipient" is whoever opened the join link; "agent" is the company's own support staff.
// Deliberately not "teacher"/"cs" - those are School Bright's words, and this product is used
// by other companies whose users are not teachers.
export type ChatSenderRole = "recipient" | "agent" | "system";

export type ChatMessage = {
  id: string;
  /** A LearningSession id. */
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

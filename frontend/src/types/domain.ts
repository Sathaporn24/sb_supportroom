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
  /** A KnowledgeCategory id - always a Level 2 (subcategory) row (TX-4). Every lesson has one;
   * companies with nothing chosen yet fall back to the "ยังไม่จัดหมวด" system-default leaf. */
  categoryId: string;
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
  slideConfigs: SlideConfig[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

/** Anonymous lesson payload: excludes source URLs, provider ids and document ids. Pacing fields
 * are declared directly as `number` (not `Pick<LessonConfig, ...>`) because they come straight
 * from the owning `Company`'s pacing settings (LP-1/LP-7) - lessons have no pacing values of
 * their own, so the learner side must never see `null` here. */
export type LearnerLessonConfig = Pick<LessonConfig, "slug" | "title" | "description" | "contentSourceType"> & {
  introWaitMs: number;
  breathPauseMs: number;
  finalQuestionWaitMs: number;
};

/** Mirrors CompanyLessonPacingViewModel - GET/PUT /api/companies/{companyId}/lesson-pacing.
 * Always NOT NULL: the company layer never has an "unset" state (LP-1). */
export type CompanyLessonPacing = {
  introWaitMs: number;
  breathPauseMs: number;
  finalQuestionWaitMs: number;
};

// presentationId is always derived server-side from slidesSourceUrl - CS never sets it directly.
export type LessonConfigInput = Omit<LessonConfig, "id" | "createdAt" | "updatedAt" | "presentationId">;

// ─── PDF narration overrides (R4/NR-1..NR-9) ──────────────────────────────────────────────────
// contentSourceType = "pdf" only - Google Slides has no override path (R4, NR-9).

/** Mirrors LessonNarrationSlideViewModel - one PDF page's resolved narration. */
export type LessonNarrationSlide = {
  slideObjectId: string;
  index: number;
  /** The text that will actually be spoken/indexed - the CS override if one exists, otherwise
   * the extracted prefill (NR-1). */
  narrationText: string;
  /** true when a LessonSlideNarration row exists for this page - tells a CS-authored page apart
   * from one still showing the extracted prefill. */
  isOverridden: boolean;
};

/** Mirrors LessonNarrationsViewModel - GET /api/lessons/{id}/narrations response. */
export type LessonNarrations = {
  slides: LessonNarrationSlide[];
  /** NR-5 - true when every page's freshly-extracted text is blank (almost always a scanned
   * PDF): a warning, not an error - narration can still be saved normally, CS just has to type
   * every page by hand. */
  isLikelyScanned: boolean;
};

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
  /** Legacy total-round count retained by the API during the aggregate rollout. */
  learningSessionCount: number;
  /** Distinct browser keys that have opened this link. */
  learnerCount: number;
  /** Learning rounds currently in progress. */
  inProgressCount: number;
  /** Learning rounds that have ended. */
  endedCount: number;
};

/** Anonymous pre-join payload: no database ids, lesson slug or attendance count. */
export type PublicTrainingLink = Pick<
  TrainingLink,
  "token" | "recipientOrgName" | "status" | "expiresAt"
>;

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
  /** null = ยังไม่เคยส่งความคืบหน้ามาเลย - ต่างจาก 0 ที่แปลว่าอยู่สไลด์แรกจริง */
  lastSlideIndex: number | null;
  /** จำนวนสไลด์ทั้ง deck ตอนที่คนนี้เรียน - คู่กับ lastSlideIndex เพื่อแสดง "7/20" */
  totalSlideCount: number | null;
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

/**
 * สิ่งที่หน้า join ต้องรู้ก่อนแสดงอะไรทั้งสิ้น - server เป็นคนตัดสิน ไม่ใช่เบราว์เซอร์
 * `resumable` ไม่ null = **ต้องถามยืนยันเสมอ** ห้ามพาเข้าห้องเงียบๆ (กุญแจอยู่ที่เบราว์เซอร์ ไม่ใช่ที่คน)
 */
export type LearningResumeState = {
  link: PublicTrainingLink;
  lessonTitle: string;
  /** การเรียนที่ยังไม่จบของเบราว์เซอร์นี้ - มีเมื่อไหร่ต้องถามก่อนเสมอ */
  resumable: LearningSession | null;
  /** รอบล่าสุดที่จบแล้ว - ดูเฉพาะตอนไม่มี resumable */
  lastEnded: LearningSession | null;
  linkExpired: boolean;
};

export type UpdateLearningProgressInput = {
  learnerKey: string;
  lastSlideObjectId?: string;
  lastSlideIndex: number;
  /** ส่งเฉพาะเมื่อ deck โหลดเสร็จแล้ว - server เขียนเฉพาะค่าที่ > 0 */
  totalSlideCount?: number;
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
  reviewResult?: ReviewResult | null;
  reviewNote?: string | null;
  reviewedAt?: string | null;
};

/** Public learner shape: answer-review metadata belongs to the back office only. */
export type LearnerSessionQuestion = Omit<
  SessionQuestion,
  "reviewResult" | "reviewNote" | "reviewedAt"
>;

export type CreateSessionQuestionInput = Omit<
  SessionQuestion,
  "id" | "createdAt" | "reviewResult" | "reviewNote" | "reviewedAt"
>;

export type ReviewSessionQuestionInput = {
  reviewResult: ReviewResult | null;
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

/** Public learner recap: no internal follow-up list and no CS review metadata. */
export type LearnerSessionSummary = Omit<SessionSummary, "questions" | "unansweredPoints"> & {
  questions: LearnerSessionQuestion[];
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
 * scopeType/scopeId say what it answers for (KnowledgeScopeType, defined below) - "company" with
 * no scopeId is the standalone/global library, queried alongside every lesson's own content.
 */
export type DocumentIndexingStatus = "pending" | "indexed" | "failed";

/** Mirrors DocumentFailureReason.cs - which of the 5 upload/index steps failed and why (R6.4).
 * Each reason needs a different fix from CS, so the UI must never collapse them into one message. */
export type DocumentFailureReason =
  | "unsupported_type"
  | "extract_failed"
  | "no_text"
  | "embedding_failed"
  | "index_failed";

/** Mirrors UploadDocumentDto/MoveDocumentScopeDto's shared shape (design.md DS-1/DS-5) - what
 * says a document or Q&A answers for one lesson, one category, or the whole company. `scopeId` is
 * required for "lesson"/"category" and forbidden for "company" (KS-2/DS-3). For "lesson" it is
 * LessonConfig.Id, never Slug (DS-1). */
export type DocumentScope = {
  scopeType: KnowledgeScopeType;
  scopeId?: string;
};

export type DocumentResource = {
  id: string;
  scopeType: KnowledgeScopeType;
  scopeId?: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  indexingStatus: DocumentIndexingStatus;
  indexedChunkCount: number;
  failureReason?: DocumentFailureReason;
  createdAt: string;
  /** DI-10 - set only while a retry is still scheduled (job pending/running, under the attempt
   * cap) - lets the UI tell "failed, but will retry itself" apart from "failed, needs a person". */
  willRetryAt?: string;
  /** R-4/DI-16 - true while a vector_delete job for this (soft-deleted) document is still
   * pending/running: the document is gone from the list, but its vectors may still answer
   * questions in Pinecone until this clears. */
  hasPendingVectorDelete: boolean;
};

/** Mirrors DocumentChunkViewModel (DI-7) - one row of GET /api/documents/{id}/chunks, exactly
 * what the knowledge store received for this document, never re-parsed on the fly. */
export type DocumentChunk = {
  id: string;
  chunkKey: string;
  seqNo: number;
  text: string;
  charCount: number;
  /** DI-6 - sort hint only, never a correctness verdict - a human still has to look. */
  hasSuspectCharacters: boolean;
};

// ─── Back office identity (TD-014) ────────────────────────────────────────────────────────────
// Only the admin surface has accounts. The person who receives a training link has none and
// never will: their token is both key and scope. Do not add a learner role here.

/** Mirrors AdminRole.cs. */
export type AdminRole = "owner" | "admin" | "cs";

/** Mirrors CompanyViewModel - one entry in the company switcher. */
export type Company = {
  id: string;
  name: string;
  isActive: boolean;
};

/** Mirrors CreateCompanyDto. Role and CompanyId are deliberately not request fields (CP-2/CP-8). */
export type CreateCompanyInput = {
  id: string;
  name: string;
  adminEmail: string;
  adminDisplayName: string;
  adminInitialPassword: string;
};

/** Mirrors SignedInUserViewModel - the signed-in user's own profile. */
export type SignedInUser = {
  id: string;
  email: string;
  displayName: string;
  role: AdminRole;
  /** null for an owner, who spans every company. */
  companyId?: string;
  /** When true the app must keep the back office out of reach until the password is changed. */
  mustChangePassword: boolean;
};

/** Mirrors LoginResultViewModel. */
export type LoginResult = {
  token: string;
  expiresAt: string;
  user: SignedInUser;
};

/**
 * Mirrors AdminUserViewModel - one row on the user-management screen. Deliberately a separate
 * type from SignedInUser: they answer different questions and will drift apart.
 */
export type AdminUser = {
  id: string;
  email: string;
  displayName: string;
  role: AdminRole;
  companyId?: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
};

export type CreateAdminUserInput = {
  email: string;
  displayName: string;
  role: AdminRole;
  /** Ignored for an owner; for anyone else a company admin's own company always wins server-side. */
  companyId?: string;
  initialPassword: string;
};

export type UpdateAdminUserInput = {
  displayName: string;
  role: AdminRole;
  isActive: boolean;
};

export type LoginInput = {
  email: string;
  password: string;
};

export type ChangePasswordInput = {
  currentPassword: string;
  newPassword: string;
};

// ─── Knowledge base taxonomy & scope (Phase 1) ────────────────────────────────────────────────
// Mirrors KnowledgeScopeType.cs string constants exactly - both DocumentResource and, from
// Phase 6 onward, KnowledgeQnA use this same union to say what a piece of knowledge answers for.

export type KnowledgeScopeType = "lesson" | "category" | "company";

/**
 * Mirrors KnowledgeCategoryViewModel. One table holds both levels via parentId (design.md DM-1):
 * level 1 (parentId null) is a top-level category, level 2 (parentId set) is its subcategory.
 * Nesting stops at level 2 - a level 2 row can never be a parent (TX-2).
 *
 * isSystemDefault marks the "ยังไม่จัดหมวด" chain every company gets backfilled with (exactly
 * one level 1 + one level 2 row) - that pair can never be renamed, deleted, or reparented (TX-11).
 */
export type KnowledgeCategory = {
  id: string;
  parentId: string | null;
  level: 1 | 2;
  name: string;
  description?: string;
  sortOrder: number;
  isSystemDefault: boolean;
};

export type CreateKnowledgeCategoryInput = {
  /** Omit to create a level 1 category; set to a level 1 category's id to create its subcategory. */
  parentId?: string;
  name: string;
  description?: string;
  sortOrder: number;
};

export type UpdateKnowledgeCategoryInput = {
  name: string;
  description?: string;
  sortOrder: number;
};

/**
 * Mirrors CategoryMovePreviewViewModel - what GET /api/knowledge-categories/{id}/move-preview
 * returns before a lesson's category actually changes (R3.1/TX-10).
 */
export type CategoryMovePreview = {
  losingDocuments: number;
  losingQnAs: number;
  gainingDocuments: number;
  gainingQnAs: number;
};

// ─── Q&A knowledge base & review queue (Phase 6 - R5/QQ-1..QQ-10) ────────────────────────────
// "คลังความรู้คือคู่ถาม-ตอบ" - CS writes the right answer directly at the point a gap was found,
// instead of authoring a document and waiting for it to be indexed (see design.md R5).

/** Mirrors KnowledgeQnAViewModel - one saved question/answer pair. */
export type KnowledgeQnA = {
  id: string;
  question: string;
  answer: string;
  scopeType: KnowledgeScopeType;
  scopeId?: string;
  indexingStatus: DocumentIndexingStatus;
  failureReason?: DocumentFailureReason;
  createdAt: string;
};

export type CreateKnowledgeQnAInput = {
  question: string;
  answer: string;
  scopeType: KnowledgeScopeType;
  scopeId?: string;
  /** QQ-7 - one Q&A can close several queue questions at once; at least one is required. */
  sessionQuestionIds: string[];
};

export type UpdateKnowledgeQnAInput = {
  question: string;
  answer: string;
};

/**
 * Mirrors KnowledgeQnAQueueItemViewModel - one row of GET /api/qna-queue (P8/R5.1). A
 * SessionQuestion (learning-session module) that has no Q&A answering it yet (QQ-1), enriched
 * with which lesson it happened in (QQ-4) and why it is here (QQ-3).
 */
export type KnowledgeQnAQueueItem = {
  id: string;
  /** A LearningSession id. */
  sessionId: string;
  slideObjectId?: string;
  transcript?: string;
  answer?: string;
  answerStatus: AnswerStatus;
  reviewResult?: ReviewResult | null;
  reviewNote?: string | null;
  createdAt: string;
  /** Absent when the training link/lesson it happened on has since been deleted. */
  lessonId?: string;
  lessonSlug?: string;
  /** QQ-3 - independent flags, not one source enum: a question can carry both at once. */
  fromNotFound: boolean;
  fromIncorrect: boolean;
};

/**
 * Mirrors KnowledgeQnAConflictViewModel (QQ-9/QQ-10/R5.5) - raised when the model reports that a
 * Q&A it retrieved conflicts with a document/slide. The document already won when answering; this
 * flag exists so CS can go fix the document that caused the mismatch.
 */
export type KnowledgeQnAConflict = {
  id: string;
  qnaId: string;
  sessionQuestionId?: string;
  conflictingSourceLabel: string;
  modelNote?: string;
  createdAt: string;
  resolvedAt?: string;
  resolvedBy?: string;
};

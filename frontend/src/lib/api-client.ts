"use client";

import type { ApiErrorResponse } from "@/types/api";
import { getAccessToken, getActiveCompanyId } from "@/lib/auth-session";
import type {
  AdminUser,
  CategoryMovePreview,
  ChangePasswordInput,
  Company,
  CompanyLessonPacing,
  CreateCompanyInput,
  CreateAdminUserInput,
  CreateKnowledgeCategoryInput,
  CreateKnowledgeQnAInput,
  CreateTrainingLinkInput,
  DocumentChunk,
  DocumentResource,
  DocumentScope,
  EndLearningSessionInput,
  JoinLearningSessionInput,
  KnowledgeCategory,
  KnowledgeQnA,
  KnowledgeQnAConflict,
  KnowledgeQnAFilter,
  KnowledgeQnAQueueItem,
  LearningSession,
  LearnerLessonConfig,
  LessonConfig,
  LessonConfigInput,
  LessonNarrationCount,
  LessonNarrations,
  LessonTrashItem,
  LoginInput,
  LoginResult,
  PdfPreviewSessionResponse,
  ResolvedPresentation,
  RequestLessonPermanentDeleteInput,
  ReviewSessionQuestionInput,
  LearnerSessionQuestion,
  LearnerSessionSummary,
  SessionQuestion,
  SessionSummary,
  SignedInUser,
  SlidesLessonContent,
  TeachingSlide,
  TrainingLink,
  LearningResumeState,
  PublicTrainingLink,
  UpdateAdminUserInput,
  UpdateKnowledgeCategoryInput,
  UpdateKnowledgeQnAInput,
  UpdateLearningProgressInput,
  VoiceQuestionResult,
} from "@/types/domain";

// The only place browser code talks to the backend - Application Services/Hooks call these
// functions, never `fetch(...)` directly. Every request goes to the SB_Ai_Supportroom .NET
// API (see apiUrl() below), not this Next.js app's own route handlers.

export class ApiClientError extends Error {
  constructor(
    public readonly response: ApiErrorResponse,
    public readonly status: number,
  ) {
    super(response.error.message);
    this.name = "ApiClientError";
  }
}

// The .NET backend (SB_Ai_Supportroom) now serves every route below - this used to be
// relative (same-origin Next.js route handlers), now cross-origin against the API's own host.
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

/**
 * Adds ?company= to back-office calls so the server knows which customer is being viewed.
 *
 * Only when signed in: the learner surface is anonymous and derives its company from the link
 * token instead, so tagging those requests would be noise at best and misleading at worst.
 *
 * The value is a hint, never a permission - the server checks on every request that this user
 * may act on that company and answers 403 if not (TD-014).
 */
function backendUrl(path: string): string {
  return `${API_BASE_URL}${path}`;
}

function apiUrl(path: string): string {
  const url = backendUrl(path);
  const companyId = getActiveCompanyId();
  if (!companyId || !getAccessToken()) return url;
  return `${url}${path.includes("?") ? "&" : "?"}company=${encodeURIComponent(companyId)}`;
}

/**
 * Called when the API says the token is missing or expired. Set by AdminSessionProvider so this
 * module can hand control back to the app without importing React or the router.
 */
let onUnauthorized: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  onUnauthorized = handler;
}

async function request<T>(input: string, init?: RequestInit, authenticate = true): Promise<T> {
  const token = authenticate ? getAccessToken() : null;
  const response = await fetch(input, {
    ...init,
    headers: token ? { ...init?.headers, Authorization: `Bearer ${token}` } : init?.headers,
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as ApiErrorResponse | null;

    // 401 only. A 403 must NOT sign anyone out: they are correctly signed in and simply asked
    // for something that is not theirs, and throwing them to a login screen would both lose
    // their work and imply that signing in again might help.
    if (authenticate && response.status === 401) {
      onUnauthorized?.();
    }

    if (body?.error) {
      throw new ApiClientError(body, response.status);
    }
    throw new Error(`Request failed: ${response.status}`);
  }

  // 204 No Content has no body to parse - changePassword returns one.
  if (response.status === 204) return undefined as T;

  return response.json() as Promise<T>;
}

/** Public learner calls must never inherit an admin JWT or ?company= from the same browser.
 * Their tenant and identity are resolved exclusively from (link token, learnerKey). */
function publicRequest<T>(path: string, init?: RequestInit): Promise<T> {
  return request(backendUrl(path), init, false);
}

// A voice question can involve transcription, retrieval, and answer generation, so its healthy
// round-trip is longer than an ordinary REST call. It must still have one user-facing ceiling:
// without this safety net a stalled upstream connection leaves the tutor in processing-question
// indefinitely, repeating waiting fillers and never reaching QUESTION_FAILED.
const QUESTION_REQUEST_TIMEOUT_MS = 45_000;

async function publicQuestionRequest<T>(path: string, init: RequestInit): Promise<T> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), QUESTION_REQUEST_TIMEOUT_MS);

  try {
    return await publicRequest(path, { ...init, signal: controller.signal });
  } finally {
    clearTimeout(timeoutId);
  }
}

const jsonHeaders = { "Content-Type": "application/json" };

export function listLessons(): Promise<{ lessons: LessonConfig[] }> {
  return request(apiUrl("/api/lessons"));
}

export function saveLesson(input: LessonConfigInput): Promise<{ lesson: LessonConfig }> {
  return request(apiUrl("/api/lessons"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

// --- Lesson trash, restore & permanent purge (R9/Module L, LT-1..LT-24) ---------------------

/** LT-7 - the normal list above never includes a trashed lesson; this is the only way to see one. */
export function listTrashedLessons(): Promise<{ lessons: LessonTrashItem[] }> {
  return request(apiUrl("/api/lessons/trash"));
}

/** LT-2/LT-3 - owner or admin only (server-enforced); revokes every TrainingLink on this lesson
 * immediately and schedules the 60-day purge job. Idempotent at the already-trashed state. */
export function archiveLesson(id: string): Promise<{ status: string }> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(id)}/trash`), { method: "POST" });
}

/** LT-2/LT-4 - owner or admin only; cancels the pending purge job and returns the lesson to
 * active. Revoked TrainingLinks are never restored (LT-4) - a fresh link is required. 409 once
 * the worker has already claimed the purge (LT-13). */
export function restoreLesson(id: string): Promise<{ status: string }> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(id)}/restore`), { method: "POST" });
}

/** LT-2/LT-10 - owner only. `confirmationTitle` must match the lesson's own title exactly (server
 * trims and compares ordinally). A 202 means the purge job was accelerated to run now, not that
 * the lesson is already gone - the worker still has to run. */
export function requestLessonPermanentDelete(
  id: string,
  input: RequestLessonPermanentDeleteInput,
): Promise<{ status: string }> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(id)}/permanent-delete`), {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** TX-9 - a category move is a single-column update, never a re-index. Call getCategoryMovePreview
 * first and get the CS's confirmation before calling this (R3.1) - never call it silently from a
 * general lesson save. */
export function moveLessonCategory(lessonId: string, categoryId: string): Promise<{ lesson: LessonConfig }> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(lessonId)}/category`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify({ categoryId }),
  });
}

// --- PDF narration overrides (R4/NR-1..NR-9 - pdf-sourced lessons only) ----------------------

/** NR-1/NR-5 - every page's resolved narration text plus the "likely scanned" warning. */
export function getLessonNarrations(lessonId: string): Promise<LessonNarrations> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(lessonId)}/narrations`));
}

/** NR-2 - an empty/omitted narrationText deletes the override row (reverts to extracted text).
 * Rejected server-side when the lesson is not PDF-sourced (NR-9). */
export function saveLessonNarration(
  lessonId: string,
  slideObjectId: string,
  narrationText: string,
): Promise<void> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(lessonId)}/narrations/${encodeURIComponent(slideObjectId)}`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify({ narrationText }),
  });
}

/** NR-3/EX-10 - how many narration overrides and how many excluded pages would be cleared if the
 * lesson's PDF source is replaced. Call before letting CS confirm uploading a new PDF over an
 * existing one. */
export function getLessonNarrationCount(lessonId: string): Promise<LessonNarrationCount> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(lessonId)}/narrations/count`));
}

/** EX-4 (R4.7) - toggle whether a PDF page is excluded from teaching/answering. Idempotent both
 * ways (design.md EX-4): setting excluded=true on an already-excluded page, or excluded=false on
 * a page that isn't excluded, is a no-op 200, not an error. Never call this during the
 * create-lesson content phase (Module J) - there is no LessonId yet (EX-9). */
export function toggleExcludedSlide(
  lessonId: string,
  slideObjectId: string,
  excluded: boolean,
): Promise<void> {
  return request(
    apiUrl(`/api/lessons/${encodeURIComponent(lessonId)}/slides/${encodeURIComponent(slideObjectId)}/excluded`),
    {
      method: "PUT",
      headers: jsonHeaders,
      body: JSON.stringify({ excluded }),
    },
  );
}

export function resolveSlides(input: {
  slidesSourceUrl: string;
  slidesEmbedUrl?: string;
}): Promise<ResolvedPresentation> {
  return request(apiUrl("/api/slides/resolve"), {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

export function getSlidesContentPreview(presentationId: string): Promise<SlidesLessonContent> {
  return request(apiUrl(`/api/slides/content?presentationId=${encodeURIComponent(presentationId)}`));
}

/** documentId must already be uploaded via uploadDocument() - collapses Google's separate
 * resolve+content steps into one, since the file is already stored. */
export function previewPdfLessonContent(documentId: string): Promise<SlidesLessonContent> {
  return request(apiUrl(`/api/lessons/pdf-preview?documentId=${encodeURIComponent(documentId)}`));
}

// --- PDF preview sessions (Module J / NR-10..NR-13 - create-lesson content phase only) --------

/** NR-10 - preview a PDF that has not been uploaded anywhere yet, parsed entirely in memory
 * server-side. Used by mode="create" + contentSourceType "pdf" before the content-management
 * phase's confirm step (NR-12); mode="edit" never calls this (NR-17). */
export function createPdfPreviewSession(file: File): Promise<PdfPreviewSessionResponse> {
  const formData = new FormData();
  formData.append("file", file);
  return request(apiUrl("/api/lessons/pdf-preview/session"), { method: "POST", body: formData });
}

/** NR-10/NR-11 - image URL for one page of the preview session above. `pageNumber` is 1-based.
 * A plain <img src> can't attach the bearer token this admin endpoint requires - pair with
 * fetchAuthenticatedImageUrl() below to actually load it. */
export function getPdfPreviewPageUrl(previewId: string, pageNumber: number): string {
  return apiUrl(`/api/lessons/pdf-preview/${encodeURIComponent(previewId)}/pages/${pageNumber}`);
}

/** NR-18 - image URL for one page of an already-persisted PDF document (admin-auth, company
 * scope from the normal query filter) - used by both the create-lesson content phase and
 * /admin/lessons/[slug]/narrations. `pageNumber` is 1-based. Same auth caveat as above. */
export function getLessonPdfPageUrl(documentId: string, pageNumber: number): string {
  return apiUrl(`/api/documents/${encodeURIComponent(documentId)}/pdf-pages/${pageNumber}`);
}

/** Admin PDF page endpoints require a Bearer token that a plain <img src> cannot send - fetch the
 * PNG with the same auth as every other admin request and hand back an object URL. The caller
 * owns its lifetime and must URL.revokeObjectURL() it once done (see SlideNarrationEditorCard). */
export async function fetchAuthenticatedImageUrl(url: string): Promise<string> {
  const token = getAccessToken();
  const response = await fetch(url, token ? { headers: { Authorization: `Bearer ${token}` } } : undefined);
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  const blob = await response.blob();
  return URL.createObjectURL(blob);
}

/** Learner-side content lookup. The token resolves company and lesson together server-side; a
 * public slug alone cannot safely identify a lesson now that slugs are unique only per company.
 * LT-5/LT-6 - `learnerKey` is required so a revoked (trashed-lesson) link can still be told apart
 * from a plain-token attempt: the server allows this call to keep working only for the session
 * that owns this exact (token, learnerKey) pair while it is still IN_PROGRESS. */
export async function getLessonByLinkToken(
  token: string,
  learnerKey: string,
): Promise<{ lesson: LearnerLessonConfig; embedUrl: string; slides: TeachingSlide[] }> {
  const result = await publicRequest<{
    lesson: LearnerLessonConfig;
    embedUrl: string;
    slides: TeachingSlide[];
  }>(`/api/lessons/by-link/${encodeURIComponent(token)}?learnerKey=${encodeURIComponent(learnerKey)}`);

  // PDF page paths are generated by the API. Prefix its host when frontend and backend are
  // deployed separately; Google-hosted absolute URLs remain untouched.
  return {
    ...result,
    slides: result.slides.map((slide) => ({
      ...slide,
      slideUrl: slide.slideUrl?.startsWith("/") ? backendUrl(slide.slideUrl) : slide.slideUrl,
    })),
  };
}

// --- Knowledge categories (2-level taxonomy - design.md DM-1/Taxonomy Rules) -----------------

/** Both levels come back together, told apart by level/parentId - not two separate lists. */
export function listKnowledgeCategories(): Promise<{ categories: KnowledgeCategory[] }> {
  return request(apiUrl("/api/knowledge-categories"));
}

export function createKnowledgeCategory(
  input: CreateKnowledgeCategoryInput,
): Promise<{ category: KnowledgeCategory }> {
  return request(apiUrl("/api/knowledge-categories"), {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** Rejected server-side (TX-11) when the target row is the isSystemDefault chain. */
export function updateKnowledgeCategory(
  id: string,
  input: UpdateKnowledgeCategoryInput,
): Promise<{ category: KnowledgeCategory }> {
  return request(apiUrl(`/api/knowledge-categories/${encodeURIComponent(id)}`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** Rejected server-side (TX-6) when the category still holds lessons/documents/Q&A/subcategories,
 * or is the isSystemDefault chain (TX-11) - the error message names each count in Thai. */
export function deleteKnowledgeCategory(id: string): Promise<{ status: string }> {
  return request(apiUrl(`/api/knowledge-categories/${encodeURIComponent(id)}`), { method: "DELETE" });
}

/** R3.1/TX-10 - what a lesson would lose/gain access to by moving from category `currentCategoryId`
 * to `targetCategoryId` (both must be Level 2). Must be shown and confirmed before calling
 * moveLessonCategory(). */
export function getCategoryMovePreview(
  currentCategoryId: string,
  targetCategoryId: string,
): Promise<CategoryMovePreview> {
  return request(
    apiUrl(
      `/api/knowledge-categories/${encodeURIComponent(currentCategoryId)}/move-preview?targetCategoryId=${encodeURIComponent(targetCategoryId)}`,
    ),
  );
}

// --- Training links (CS creates, one link serves many people) -------------------------------

export function listTrainingLinks(): Promise<{ links: TrainingLink[] }> {
  return request(apiUrl("/api/training-links"));
}

export function createTrainingLink(input: CreateTrainingLinkInput): Promise<{ link: TrainingLink }> {
  return request(apiUrl("/api/training-links"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

/** What the join screen loads before anyone has typed a name. */
export function getTrainingLinkByToken(token: string): Promise<{ link: PublicTrainingLink; lessonTitle: string }> {
  return publicRequest(`/api/training-links/${encodeURIComponent(token)}`);
}

/** Back-office version keeps ids/counts and enforces JWT + selected-company scope. */
export function getAdminTrainingLinkByToken(
  token: string,
): Promise<{ link: TrainingLink; lessonTitle: string }> {
  return request(apiUrl(`/api/training-links/by-token/${encodeURIComponent(token)}`));
}

/** CS drill-down: everyone who has opened one link. */
export function listLearningSessionsForLink(
  linkId: string,
): Promise<{ learningSessions: LearningSession[] }> {
  return request(apiUrl(`/api/training-links/${encodeURIComponent(linkId)}/learning-sessions`));
}

// --- Learning sessions (one person's run) ---------------------------------------------------
//
// Every recipient-side call carries the link token AND the browser's learnerKey. The token says
// which lesson and which company; the key says which of the many people on that link is calling.
// Sending only the token would let any learner read every other learner's data on the same link.

/** Idempotent - a browser that already has a session on this link gets it back, which is what
 * makes reconnecting free. */
export function joinLearningSession(
  token: string,
  input: JoinLearningSessionInput,
): Promise<{ learningSession: LearningSession }> {
  return publicRequest(`/api/learning-sessions/${encodeURIComponent(token)}/join`, {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** "เรียนอีกครั้ง" - explicitly a new round, not a reopen of the finished one. */
export function restartLearningSession(
  token: string,
  input: JoinLearningSessionInput,
): Promise<{ learningSession: LearningSession }> {
  return publicRequest(`/api/learning-sessions/${encodeURIComponent(token)}/restart`, {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/**
 * ถามก่อนเข้าห้องเสมอ: เบราว์เซอร์นี้มีการเรียนค้างอยู่ไหม และเคยเรียนจบไปแล้วหรือยัง
 * เป็น read-only ไม่สร้างแถวใหม่ - การตัดสินใจ "เรียนต่อ" เป็นของผู้ใช้บนหน้าจอ ไม่ใช่ของระบบ
 */
export function getLearningResumeState(
  token: string,
  learnerKey: string | null,
): Promise<LearningResumeState> {
  const query = learnerKey ? `?learnerKey=${encodeURIComponent(learnerKey)}` : "";
  return publicRequest(`/api/learning-sessions/${encodeURIComponent(token)}/resume${query}`);
}

export function updateLearningProgress(
  token: string,
  input: UpdateLearningProgressInput,
): Promise<{ learningSession: LearningSession }> {
  return publicRequest(`/api/learning-sessions/${encodeURIComponent(token)}/progress`, {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

export function endLearningSession(
  token: string,
  input: EndLearningSessionInput,
): Promise<{ learningSession: LearningSession }> {
  return publicRequest(`/api/learning-sessions/${encodeURIComponent(token)}/end`, {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** The learner's own recap. Internal review fields and unansweredPoints are absent by contract. */
export function getOwnLearningSummary(
  token: string,
  learnerKey: string,
): Promise<{ learningSession: LearningSession; summary: LearnerSessionSummary }> {
  return publicRequest(
    `/api/learning-sessions/${encodeURIComponent(token)}/summary?learnerKey=${encodeURIComponent(learnerKey)}`,
  );
}

/** CS-facing: any learning session's full summary by id. */
export function getLearningSummaryById(learningSessionId: string): Promise<{ summary: SessionSummary }> {
  return request(apiUrl(`/api/learning-sessions/${encodeURIComponent(learningSessionId)}/summary/by-id`));
}

// --- Questions ---------------------------------------------------------------------------------

export function listOwnQuestions(
  token: string,
  learnerKey: string,
): Promise<{ questions: LearnerSessionQuestion[] }> {
  return publicRequest(
    `/api/session-questions?token=${encodeURIComponent(token)}&learnerKey=${encodeURIComponent(learnerKey)}`,
  );
}

export function listQuestionsByLearningSession(
  learningSessionId: string,
): Promise<{ questions: SessionQuestion[] }> {
  return request(
    apiUrl(`/api/session-questions/by-learning-session/${encodeURIComponent(learningSessionId)}`),
  );
}

export function reviewSessionQuestion(
  questionId: string,
  input: ReviewSessionQuestionInput,
): Promise<{ question: SessionQuestion }> {
  return request(apiUrl(`/api/session-questions/${encodeURIComponent(questionId)}/review`), {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** `rate` is an SSML percentage ("-45%") for utterances that shouldn't run at lesson pace. */
export async function synthesizeSpeech(
  text: string,
  token: string,
  learnerKey: string,
  rate?: string,
): Promise<Blob> {
  const response = await fetch(backendUrl("/api/tts"), {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(rate ? { text, token, learnerKey, rate } : { text, token, learnerKey }),
  });
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as ApiErrorResponse | null;
    throw new Error(body?.error?.message ?? "แปลงข้อความเป็นเสียงไม่สำเร็จ");
  }
  return response.blob();
}

export function askVoiceQuestion(input: {
  audioBlob: Blob;
  /** The link token. The backend derives company and lesson from it - sending a lesson slug
   * separately would let the two disagree. */
  token: string;
  /** Which learner on that link is asking. Without it the answer would be filed under, and
   * broadcast to, the wrong person. */
  learnerKey: string;
  currentSlideObjectId?: string;
  durationMs: number;
}): Promise<VoiceQuestionResult> {
  const formData = new FormData();
  formData.append("audio", input.audioBlob, "question.webm");
  formData.append("token", input.token);
  formData.append("learnerKey", input.learnerKey);
  formData.append("durationMs", String(input.durationMs));
  if (input.currentSlideObjectId) {
    formData.append("currentSlideObjectId", input.currentSlideObjectId);
  }
  return publicQuestionRequest("/api/voice-question", { method: "POST", body: formData });
}

/** TQ-13 - the typed-question equivalent of askVoiceQuestion. Separate JSON POST rather than a
 * shared function: the two channels have nothing in common at the transport layer (multipart
 * audio upload vs a plain JSON body), same reasoning as TextQuestionController being a separate
 * controller from VoiceQuestionController on the backend. */
export function askTextQuestion(input: {
  token: string;
  learnerKey: string;
  text: string;
  currentSlideObjectId?: string;
}): Promise<VoiceQuestionResult> {
  return publicQuestionRequest("/api/text-question", {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

export function resetDemoData(): Promise<{ status: string }> {
  return request(apiUrl("/api/admin/reset"), { method: "POST" });
}

/** DS-1/KL-21 - scope is required, never inferred: pass { scopeType: "company" } for the
 * standalone library, { scopeType: "lesson", scopeId: LessonConfig.Id } to attach to one lesson,
 * or { scopeType: "category", scopeId } for a Level-2 category. `checkDuplicate` defaults to
 * `false` (unchanged behaviour everywhere except the library page's own upload form, KL-21) -
 * when `true` and the server finds a match it responds 409 with a normal ApiClientError whose
 * `response.error.details` is a DuplicateDocumentsResponse (KL-21/KL-22); nothing is written. */
export function uploadDocument(
  file: File,
  scope: DocumentScope,
  checkDuplicate = false,
): Promise<{ document: DocumentResource }> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("scopeType", scope.scopeType);
  if (scope.scopeId) {
    formData.append("scopeId", scope.scopeId);
  }
  if (checkDuplicate) {
    formData.append("checkDuplicate", "true");
  }
  return request(apiUrl("/api/documents"), { method: "POST", body: formData });
}

function libraryFilterParams(filter: KnowledgeQnAFilter): URLSearchParams {
  const params = new URLSearchParams();
  if (filter.scopeType) params.set("scopeType", filter.scopeType);
  if (filter.scopeId) params.set("scopeId", filter.scopeId);
  if (filter.status) params.set("status", filter.status);
  if (filter.q) params.set("q", filter.q);
  return params;
}

/** KL-2 - omitting scopeType (pass `{}` or nothing) returns every scope for the company, the
 * library page's default. Pass an explicit scopeType for the old single-scope behaviour (still
 * used when embedded in a lesson editor). */
export function listDocuments(filter: KnowledgeQnAFilter = {}): Promise<{ documents: DocumentResource[] }> {
  const query = libraryFilterParams(filter).toString();
  return request(apiUrl(`/api/documents${query ? `?${query}` : ""}`));
}

/** DS-5/DS-6 - moves an already-uploaded document to a new scope; the server re-embeds into the
 * new namespace and deletes vectors from the old one (KS-4: a scope change is a move, not a
 * column update). */
export function moveDocumentScope(id: string, scope: DocumentScope): Promise<{ document: DocumentResource }> {
  return request(apiUrl(`/api/documents/${encodeURIComponent(id)}/scope`), {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify(scope),
  });
}

/** R6.1/DI-15 - soft-deleted documents still recoverable (file kept in object storage). */
export function listDeletedDocuments(): Promise<{ documents: DocumentResource[] }> {
  return request(apiUrl("/api/documents/deleted"));
}

export function deleteDocument(id: string): Promise<{ status: string }> {
  return request(apiUrl(`/api/documents/${encodeURIComponent(id)}`), { method: "DELETE" });
}

/** DI-15 - re-extracts and re-indexes from the file still in object storage; spends embedding
 * cost again. */
export function restoreDocument(id: string): Promise<{ status: string }> {
  return request(apiUrl(`/api/documents/${encodeURIComponent(id)}/restore`), { method: "POST" });
}

/** DI-7 - every chunk the knowledge store received for this document, ordered by seqNo. The
 * extracted-text-visibility screen's data source - first endpoint in the system returning raw
 * uploaded-file content. */
export function getDocumentChunks(id: string): Promise<{ chunks: DocumentChunk[] }> {
  return request(apiUrl(`/api/documents/${encodeURIComponent(id)}/chunks`));
}

/** Base URL the SignalR hub connection should use - same host as every REST call above. */
export function getApiBaseUrl(): string {
  return API_BASE_URL;
}

// ─── Back office identity (TD-014) ────────────────────────────────────────────────────────────

export function login(input: LoginInput): Promise<{ result: LoginResult }> {
  return publicRequest("/api/auth/login", { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function getSignedInUser(): Promise<{ user: SignedInUser }> {
  return request(apiUrl("/api/auth/me"));
}

export function changePassword(input: ChangePasswordInput): Promise<void> {
  return request(apiUrl("/api/auth/change-password"), {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** Feeds the company switcher: every active company for an owner, exactly one for anyone else. */
export function listSwitchableCompanies(): Promise<{ companies: Company[] }> {
  return request(apiUrl("/api/companies"));
}

/** Owner registry: unlike the switcher endpoint above, this includes inactive companies (CP-13). */
export function listAllCompanies(): Promise<{ companies: Company[] }> {
  return request(apiUrl("/api/companies/all"));
}

export function createCompany(input: CreateCompanyInput): Promise<{ company: Company }> {
  return request(apiUrl("/api/companies"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function updateCompany(id: string, input: { name: string; isActive: boolean }): Promise<{ company: Company }> {
  return request(apiUrl(`/api/companies/${encodeURIComponent(id)}`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** LP-9 - used only by the `/admin/settings` page (company-level pacing has no lesson-level
 * counterpart to feed). Response is the plain CompanyLessonPacingViewModel, not wrapped in
 * `{ company: ... }` like the other endpoints. */
export function getCompanyLessonPacing(companyId: string): Promise<CompanyLessonPacing> {
  return request(apiUrl(`/api/companies/${encodeURIComponent(companyId)}/lesson-pacing`));
}

/** LP-9 - all three fields required, no partial update (UpdateCompanyLessonPacingDto). Rejected
 * with 403 for `cs` server-side (SP-4/SP-15). */
export function updateCompanyLessonPacing(
  companyId: string,
  payload: CompanyLessonPacing,
): Promise<CompanyLessonPacing> {
  return request(apiUrl(`/api/companies/${encodeURIComponent(companyId)}/lesson-pacing`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(payload),
  });
}

export function listAdminUsers(companyId: string): Promise<{ users: AdminUser[] }> {
  return request(apiUrl(`/api/admin-users/${encodeURIComponent(companyId)}`));
}

export function createAdminUser(input: CreateAdminUserInput): Promise<{ user: AdminUser }> {
  return request(apiUrl("/api/admin-users"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function updateAdminUser(id: string, input: UpdateAdminUserInput): Promise<{ user: AdminUser }> {
  return request(apiUrl(`/api/admin-users/${encodeURIComponent(id)}`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

// --- Q&A knowledge base & review queue (Phase 6 - design.md R5/QQ-1..QQ-10) ------------------

/** P8/R5.1/QQ-1/QQ-4 - the whole review queue in one call, across every learning session and
 * lesson. A row leaves this list only by saving a Q&A against it - there is no "dismiss" call. */
export function getQnaQueue(): Promise<{ queue: KnowledgeQnAQueueItem[] }> {
  return request(apiUrl("/api/qna-queue"));
}

/** KL-8/KL-9 - the Q&A half of the library page (`/admin/documents`); same filter shape and same
 * interpretation as listDocuments (KL-2..KL-5, KL-11..KL-13). */
export function listKnowledgeQnA(filter: KnowledgeQnAFilter = {}): Promise<{ qnas: KnowledgeQnA[] }> {
  const query = libraryFilterParams(filter).toString();
  return request(apiUrl(`/api/knowledge-qna${query ? `?${query}` : ""}`));
}

/** QQ-7/QQ-8 - closes every sessionQuestionId selected from the queue. Used immediately, no
 * approval step (R5.7). KL-23/KL-26 (mati Q-H2) - the duplicate check runs unconditionally before
 * anything is written: when the Question matches an existing, non-deleted Q&A of this company, the
 * server writes nothing and responds 409 with a normal ApiClientError whose `response.error.details`
 * is a DuplicateQnAResponse (same envelope shape as uploadDocument's KL-21 409, different details
 * type). Pass `confirmDuplicate: true` to skip the check and always succeed. */
export function createKnowledgeQnA(input: CreateKnowledgeQnAInput): Promise<{ qna: KnowledgeQnA }> {
  return request(apiUrl("/api/knowledge-qna"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

/** QQ-6 - editing never reopens the question(s) this Q&A already closed. */
export function updateKnowledgeQnA(id: string, input: UpdateKnowledgeQnAInput): Promise<{ qna: KnowledgeQnA }> {
  return request(apiUrl(`/api/knowledge-qna/${encodeURIComponent(id)}`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** QQ-5 - the question(s) this Q&A closed fall back into the review queue immediately. */
export function deleteKnowledgeQnA(id: string): Promise<{ status: string }> {
  return request(apiUrl(`/api/knowledge-qna/${encodeURIComponent(id)}`), { method: "DELETE" });
}

/** QQ-10 - open conflict flags only; there is no screen for already-closed ones today. */
export function listQnaConflicts(): Promise<{ conflicts: KnowledgeQnAConflict[] }> {
  return request(apiUrl("/api/knowledge-qna-conflicts?resolved=false"));
}

export function resolveQnaConflict(id: string): Promise<{ conflict: KnowledgeQnAConflict }> {
  return request(apiUrl(`/api/knowledge-qna-conflicts/${encodeURIComponent(id)}/resolve`), { method: "PUT" });
}

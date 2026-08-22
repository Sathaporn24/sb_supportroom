"use client";

import type { ApiErrorResponse } from "@/types/api";
import { getAccessToken, getActiveCompanyId } from "@/lib/auth-session";
import type {
  AdminUser,
  CategoryMovePreview,
  ChangePasswordInput,
  ChatMessage,
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
  KnowledgeQnAQueueItem,
  LearningSession,
  LearnerLessonConfig,
  LessonConfig,
  LessonConfigInput,
  LessonNarrations,
  LoginInput,
  LoginResult,
  ResolvedPresentation,
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

const jsonHeaders = { "Content-Type": "application/json" };

export function listLessons(): Promise<{ lessons: LessonConfig[] }> {
  return request(apiUrl("/api/lessons"));
}

export function saveLesson(input: LessonConfigInput): Promise<{ lesson: LessonConfig }> {
  return request(apiUrl("/api/lessons"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
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

/** NR-3 - how many narration overrides would be deleted if the lesson's PDF source is replaced.
 * Call before letting CS confirm uploading a new PDF over an existing one. */
export function getLessonNarrationCount(lessonId: string): Promise<{ count: number }> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(lessonId)}/narrations/count`));
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

/** Learner-side content lookup. The token resolves company and lesson together server-side; a
 * public slug alone cannot safely identify a lesson now that slugs are unique only per company. */
export async function getLessonByLinkToken(
  token: string,
): Promise<{ lesson: LearnerLessonConfig; embedUrl: string; slides: TeachingSlide[] }> {
  const result = await publicRequest<{
    lesson: LearnerLessonConfig;
    embedUrl: string;
    slides: TeachingSlide[];
  }>(`/api/lessons/by-link/${encodeURIComponent(token)}`);

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

// --- Questions and chat ---------------------------------------------------------------------

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

export function getOwnChatMessages(token: string, learnerKey: string): Promise<{ messages: ChatMessage[] }> {
  return publicRequest(
    `/api/chat-messages?token=${encodeURIComponent(token)}&learnerKey=${encodeURIComponent(learnerKey)}`,
  );
}

export function getChatMessagesByLearningSession(
  learningSessionId: string,
): Promise<{ messages: ChatMessage[] }> {
  return request(
    apiUrl(`/api/chat-messages/by-learning-session/${encodeURIComponent(learningSessionId)}`),
  );
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
  /** "readiness" answers the start prompt; omitted means a normal lesson question. */
  expecting?: "question" | "readiness";
}): Promise<VoiceQuestionResult> {
  const formData = new FormData();
  formData.append("audio", input.audioBlob, "question.webm");
  formData.append("token", input.token);
  formData.append("learnerKey", input.learnerKey);
  formData.append("durationMs", String(input.durationMs));
  if (input.expecting) {
    formData.append("expecting", input.expecting);
  }
  if (input.currentSlideObjectId) {
    formData.append("currentSlideObjectId", input.currentSlideObjectId);
  }
  return publicRequest("/api/voice-question", { method: "POST", body: formData });
}

export function resetDemoData(): Promise<{ status: string }> {
  return request(apiUrl("/api/admin/reset"), { method: "POST" });
}

/** DS-1 - scope is required, never inferred: pass { scopeType: "company" } for the standalone
 * library, { scopeType: "lesson", scopeId: LessonConfig.Id } to attach to one lesson, or
 * { scopeType: "category", scopeId } for a Level-2 category. */
export function uploadDocument(file: File, scope: DocumentScope): Promise<{ document: DocumentResource }> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("scopeType", scope.scopeType);
  if (scope.scopeId) {
    formData.append("scopeId", scope.scopeId);
  }
  return request(apiUrl("/api/documents"), { method: "POST", body: formData });
}

/** DS-4 - defaults to the company-wide library when no scope is passed. */
export function listDocuments(scope: DocumentScope = { scopeType: "company" }): Promise<{ documents: DocumentResource[] }> {
  const params = new URLSearchParams({ scopeType: scope.scopeType });
  if (scope.scopeId) {
    params.set("scopeId", scope.scopeId);
  }
  return request(apiUrl(`/api/documents?${params.toString()}`));
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

/** QQ-7/QQ-8 - closes every sessionQuestionId selected from the queue. Used immediately, no
 * approval step (R5.7). */
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

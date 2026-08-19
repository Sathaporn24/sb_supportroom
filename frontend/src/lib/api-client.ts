"use client";

import type { ApiErrorResponse } from "@/types/api";
import { getAccessToken, getActiveCompanyId } from "@/lib/auth-session";
import type {
  AdminUser,
  ChangePasswordInput,
  ChatMessage,
  Company,
  CreateAdminUserInput,
  CreateTrainingLinkInput,
  DocumentResource,
  EndLearningSessionInput,
  JoinLearningSessionInput,
  LearningSession,
  LearnerLessonConfig,
  LessonConfig,
  LessonConfigInput,
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

/** Omit lessonSlug to store as a standalone document, queried alongside every lesson's own
 * content instead of being tied to one. */
export function uploadDocument(file: File, lessonSlug?: string): Promise<{ document: DocumentResource }> {
  const formData = new FormData();
  formData.append("file", file);
  if (lessonSlug) {
    formData.append("lessonSlug", lessonSlug);
  }
  return request(apiUrl("/api/documents"), { method: "POST", body: formData });
}

/** Omit lessonSlug to list standalone documents; pass it to list one lesson's attachments. */
export function listDocuments(lessonSlug?: string): Promise<{ documents: DocumentResource[] }> {
  const query = lessonSlug ? `?lessonSlug=${encodeURIComponent(lessonSlug)}` : "";
  return request(apiUrl(`/api/documents${query}`));
}

export function deleteDocument(id: string): Promise<{ status: string }> {
  return request(apiUrl(`/api/documents/${encodeURIComponent(id)}`), { method: "DELETE" });
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

export function createCompany(input: { id: string; name: string }): Promise<{ company: Company }> {
  return request(apiUrl("/api/companies"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function updateCompany(id: string, input: { name: string; isActive: boolean }): Promise<{ company: Company }> {
  return request(apiUrl(`/api/companies/${encodeURIComponent(id)}`), {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(input),
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

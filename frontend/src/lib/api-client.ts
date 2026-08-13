"use client";

import type { ApiErrorResponse } from "@/types/api";
import type {
  ChatMessage,
  CreateTrainingLinkInput,
  DocumentResource,
  EndLearningSessionInput,
  JoinLearningSessionInput,
  LearningSession,
  LessonConfig,
  LessonConfigInput,
  ResolvedPresentation,
  ReviewSessionQuestionInput,
  SessionQuestion,
  SessionSummary,
  SlidesLessonContent,
  TeachingSlide,
  TrainingLink,
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

function apiUrl(path: string): string {
  return `${API_BASE_URL}${path}`;
}

async function request<T>(input: string, init?: RequestInit): Promise<T> {
  const response = await fetch(input, init);
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as ApiErrorResponse | null;
    if (body?.error) {
      throw new ApiClientError(body, response.status);
    }
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json() as Promise<T>;
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

export function getLessonBySlug(
  slug: string,
): Promise<{ lesson: LessonConfig; embedUrl: string; slides: TeachingSlide[] }> {
  return request(apiUrl(`/api/lessons/${encodeURIComponent(slug)}`));
}

// --- Training links (CS creates, one link serves many people) -------------------------------

export function listTrainingLinks(): Promise<{ links: TrainingLink[] }> {
  return request(apiUrl("/api/training-links"));
}

export function createTrainingLink(input: CreateTrainingLinkInput): Promise<{ link: TrainingLink }> {
  return request(apiUrl("/api/training-links"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

/** What the join screen loads before anyone has typed a name. */
export function getTrainingLinkByToken(token: string): Promise<{ link: TrainingLink; lessonTitle: string }> {
  return request(apiUrl(`/api/training-links/${encodeURIComponent(token)}`));
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
  return request(apiUrl(`/api/learning-sessions/${encodeURIComponent(token)}/join`), {
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
  return request(apiUrl(`/api/learning-sessions/${encodeURIComponent(token)}/restart`), {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

export function updateLearningProgress(
  token: string,
  input: UpdateLearningProgressInput,
): Promise<{ learningSession: LearningSession }> {
  return request(apiUrl(`/api/learning-sessions/${encodeURIComponent(token)}/progress`), {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

export function endLearningSession(
  token: string,
  input: EndLearningSessionInput,
): Promise<{ learningSession: LearningSession }> {
  return request(apiUrl(`/api/learning-sessions/${encodeURIComponent(token)}/end`), {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify(input),
  });
}

/** The learner's own recap. ⚠️ summary.unansweredPoints is internal - don't render it here. */
export function getOwnLearningSummary(
  token: string,
  learnerKey: string,
): Promise<{ learningSession: LearningSession; summary: SessionSummary }> {
  return request(
    apiUrl(
      `/api/learning-sessions/${encodeURIComponent(token)}/summary?learnerKey=${encodeURIComponent(learnerKey)}`,
    ),
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
): Promise<{ questions: SessionQuestion[] }> {
  return request(
    apiUrl(
      `/api/session-questions?token=${encodeURIComponent(token)}&learnerKey=${encodeURIComponent(learnerKey)}`,
    ),
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
  return request(
    apiUrl(
      `/api/chat-messages?token=${encodeURIComponent(token)}&learnerKey=${encodeURIComponent(learnerKey)}`,
    ),
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
export async function synthesizeSpeech(text: string, rate?: string): Promise<Blob> {
  const response = await fetch(apiUrl("/api/tts"), {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(rate ? { text, rate } : { text }),
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
  return request(apiUrl("/api/voice-question"), { method: "POST", body: formData });
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

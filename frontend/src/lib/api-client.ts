"use client";

import type { ApiErrorResponse } from "@/types/api";
import type {
  ChatMessage,
  DocumentResource,
  LessonConfig,
  LessonConfigInput,
  ResolvedPresentation,
  SessionQuestion,
  SessionSummary,
  SlidesLessonContent,
  TeachingSlide,
  TrainingSession,
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

export function listSessions(): Promise<{ sessions: TrainingSession[] }> {
  return request(apiUrl("/api/sessions"));
}

export function createSession(input: {
  lessonSlug: string;
  teacherName?: string;
  schoolName?: string;
  expiresAt?: string;
}): Promise<{ session: TrainingSession }> {
  return request(apiUrl("/api/sessions"), { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function getSessionByToken(token: string): Promise<{ session: TrainingSession; lessonTitle: string }> {
  return request(apiUrl(`/api/sessions/${encodeURIComponent(token)}`));
}

export function markSessionStarted(token: string): Promise<{ session: TrainingSession }> {
  return request(apiUrl(`/api/sessions/${encodeURIComponent(token)}`), {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify({ action: "start" }),
  });
}

export function endSession(
  token: string,
  input: { completedAllSlides: boolean; lastSlideObjectId?: string },
): Promise<{ session: TrainingSession }> {
  return request(apiUrl(`/api/sessions/${encodeURIComponent(token)}`), {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify({ action: "end", ...input }),
  });
}

export function getSessionSummary(
  token: string,
): Promise<{ session: TrainingSession; summary: SessionSummary | null }> {
  return request(apiUrl(`/api/sessions/${encodeURIComponent(token)}/summary`));
}

export function listSessionQuestions(sessionId: string): Promise<{ questions: SessionQuestion[] }> {
  return request(apiUrl(`/api/session-questions?sessionId=${encodeURIComponent(sessionId)}`));
}

export function getChatMessages(sessionId: string): Promise<{ messages: ChatMessage[] }> {
  return request(apiUrl(`/api/chat-messages?sessionId=${encodeURIComponent(sessionId)}`));
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
  lessonSlug: string;
  sessionId: string;
  currentSlideObjectId?: string;
  durationMs: number;
  /** "readiness" answers the start prompt; omitted means a normal lesson question. */
  expecting?: "question" | "readiness";
}): Promise<VoiceQuestionResult> {
  const formData = new FormData();
  formData.append("audio", input.audioBlob, "question.webm");
  formData.append("lessonSlug", input.lessonSlug);
  formData.append("sessionId", input.sessionId);
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

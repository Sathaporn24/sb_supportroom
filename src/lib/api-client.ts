"use client";

import type { ApiErrorResponse } from "@/types/api";
import type {
  LessonConfig,
  LessonConfigInput,
  SessionQuestion,
  SessionSummary,
  SlidesLessonContent,
  TeachingSlide,
  TrainingSession,
} from "@/types/domain";
import type { ResolvedPresentation } from "@/providers/slides/types";
import type { VoiceQuestionResult } from "@/providers/voice-question/types";

// The only place browser code talks to the backend - Application Services/Hooks call
// these functions, never `fetch("/api/...")` directly, and Route Handlers are the only
// thing that ever touches a Provider/Repository. See docs/API_CONTRACT.md.

export class ApiClientError extends Error {
  constructor(
    public readonly response: ApiErrorResponse,
    public readonly status: number,
  ) {
    super(response.error.message);
    this.name = "ApiClientError";
  }
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
  return request("/api/lessons");
}

export function saveLesson(input: LessonConfigInput): Promise<{ lesson: LessonConfig }> {
  return request("/api/lessons", { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function resolveSlides(input: {
  slidesSourceUrl: string;
  slidesEmbedUrl?: string;
}): Promise<ResolvedPresentation> {
  return request("/api/slides/resolve", { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function getSlidesContentPreview(presentationId: string): Promise<SlidesLessonContent> {
  return request(`/api/slides/content?presentationId=${encodeURIComponent(presentationId)}`);
}

export function getLessonBySlug(
  slug: string,
): Promise<{ lesson: LessonConfig; embedUrl: string; slides: TeachingSlide[] }> {
  return request(`/api/lessons/${encodeURIComponent(slug)}`);
}

export function listSessions(): Promise<{ sessions: TrainingSession[] }> {
  return request("/api/sessions");
}

export function createSession(input: {
  lessonSlug: string;
  teacherName?: string;
  schoolName?: string;
  expiresAt?: string;
}): Promise<{ session: TrainingSession }> {
  return request("/api/sessions", { method: "POST", headers: jsonHeaders, body: JSON.stringify(input) });
}

export function getSessionByToken(token: string): Promise<{ session: TrainingSession; lessonTitle: string }> {
  return request(`/api/sessions/${encodeURIComponent(token)}`);
}

export function markSessionStarted(token: string): Promise<{ session: TrainingSession }> {
  return request(`/api/sessions/${encodeURIComponent(token)}`, {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify({ action: "start" }),
  });
}

export function endSession(
  token: string,
  input: { completedAllSlides: boolean; lastSlideObjectId?: string },
): Promise<{ session: TrainingSession }> {
  return request(`/api/sessions/${encodeURIComponent(token)}`, {
    method: "PATCH",
    headers: jsonHeaders,
    body: JSON.stringify({ action: "end", ...input }),
  });
}

export function getSessionSummary(
  token: string,
): Promise<{ session: TrainingSession; summary: SessionSummary | null }> {
  return request(`/api/sessions/${encodeURIComponent(token)}/summary`);
}

export function listSessionQuestions(sessionId: string): Promise<{ questions: SessionQuestion[] }> {
  return request(`/api/session-questions?sessionId=${encodeURIComponent(sessionId)}`);
}

export async function synthesizeSpeech(text: string): Promise<Blob> {
  const response = await fetch("/api/tts", { method: "POST", headers: jsonHeaders, body: JSON.stringify({ text }) });
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
}): Promise<VoiceQuestionResult> {
  const formData = new FormData();
  formData.append("audio", input.audioBlob, "question.webm");
  formData.append("lessonSlug", input.lessonSlug);
  formData.append("sessionId", input.sessionId);
  formData.append("durationMs", String(input.durationMs));
  if (input.currentSlideObjectId) {
    formData.append("currentSlideObjectId", input.currentSlideObjectId);
  }
  return request("/api/voice-question", { method: "POST", body: formData });
}

export function resetDemoData(): Promise<{ status: string }> {
  return request("/api/admin/reset", { method: "POST" });
}

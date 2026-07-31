import "server-only";
import type { SessionRepository } from "@/providers/data/repository-types";
import type { CreateSessionInput, EndSessionInput, SessionStatus, TrainingSession } from "@/types/domain";
import { getSupabaseServiceClient } from "@/providers/data/supabase/client";
import { generateId, generatePublicToken } from "@/utils/id";

type SessionRow = {
  id: string;
  token: string;
  lesson_id: string;
  lesson_slug: string;
  teacher_name: string | null;
  school_name: string | null;
  status: SessionStatus;
  expires_at: string;
  started_at: string | null;
  ended_at: string | null;
  completed_all_slides: boolean;
  last_slide_object_id: string | null;
  created_at: string;
};

function toSession(row: SessionRow): TrainingSession {
  return {
    id: row.id,
    token: row.token,
    lessonId: row.lesson_id,
    lessonSlug: row.lesson_slug,
    teacherName: row.teacher_name ?? undefined,
    schoolName: row.school_name ?? undefined,
    status: row.status,
    createdAt: row.created_at,
    expiresAt: row.expires_at,
    startedAt: row.started_at ?? undefined,
    endedAt: row.ended_at ?? undefined,
    completedAllSlides: row.completed_all_slides,
    lastSlideObjectId: row.last_slide_object_id ?? undefined,
  };
}

// PREPARED — CREDENTIALS REQUIRED. Not tested against a real Supabase project yet.
export class SupabaseSessionRepository implements SessionRepository {
  async create(input: CreateSessionInput): Promise<TrainingSession> {
    const supabase = getSupabaseServiceClient();
    const { data: lesson, error: lessonError } = await supabase
      .from("lessons")
      .select("id, slug")
      .eq("slug", input.lessonSlug)
      .maybeSingle();
    if (lessonError) throw lessonError;
    if (!lesson) throw new Error(`Unknown lesson slug: ${input.lessonSlug}`);

    const { data, error } = await supabase
      .from("training_sessions")
      .insert({
        id: generateId("session"),
        token: generatePublicToken(),
        lesson_id: lesson.id,
        lesson_slug: lesson.slug,
        teacher_name: input.teacherName ?? null,
        school_name: input.schoolName ?? null,
        status: "NOT_STARTED",
        expires_at: input.expiresAt,
        completed_all_slides: false,
      })
      .select()
      .single();
    if (error) throw error;
    return toSession(data as SessionRow);
  }

  async getByToken(token: string): Promise<TrainingSession | null> {
    const supabase = getSupabaseServiceClient();
    const { data, error } = await supabase.from("training_sessions").select("*").eq("token", token).maybeSingle();
    if (error) throw error;
    return data ? toSession(data as SessionRow) : null;
  }

  async getById(id: string): Promise<TrainingSession | null> {
    const supabase = getSupabaseServiceClient();
    const { data, error } = await supabase.from("training_sessions").select("*").eq("id", id).maybeSingle();
    if (error) throw error;
    return data ? toSession(data as SessionRow) : null;
  }

  async list(): Promise<TrainingSession[]> {
    const supabase = getSupabaseServiceClient();
    const { data, error } = await supabase
      .from("training_sessions")
      .select("*")
      .order("created_at", { ascending: false });
    if (error) throw error;
    return (data ?? []).map((row) => toSession(row as SessionRow));
  }

  async markStarted(sessionId: string): Promise<void> {
    const supabase = getSupabaseServiceClient();
    const { error } = await supabase
      .from("training_sessions")
      .update({ status: "IN_PROGRESS", started_at: new Date().toISOString() })
      .eq("id", sessionId)
      .is("started_at", null);
    if (error) throw error;
  }

  async end(sessionId: string, result: EndSessionInput): Promise<void> {
    const supabase = getSupabaseServiceClient();
    const { error } = await supabase
      .from("training_sessions")
      .update({
        status: "ENDED",
        ended_at: new Date().toISOString(),
        completed_all_slides: result.completedAllSlides,
        last_slide_object_id: result.lastSlideObjectId ?? null,
      })
      .eq("id", sessionId);
    if (error) throw error;
  }
}

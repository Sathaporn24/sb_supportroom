import "server-only";
import type { SessionSummaryRepository } from "@/providers/data/repository-types";
import type { SessionQuestion, SessionSummary } from "@/types/domain";
import { getSupabaseServiceClient } from "@/providers/data/supabase/client";

type ResultRow = {
  session_id: string;
  completed_all_slides: boolean;
  last_slide_object_id: string | null;
  questions_summary: SessionQuestion[] | null;
  unanswered_points: string[] | null;
  created_at: string;
};

function toSummary(row: ResultRow): SessionSummary {
  return {
    sessionId: row.session_id,
    completedAllSlides: row.completed_all_slides,
    lastSlideObjectId: row.last_slide_object_id ?? undefined,
    questions: row.questions_summary ?? [],
    unansweredPoints: row.unanswered_points ?? [],
    createdAt: row.created_at,
  };
}

// PREPARED — CREDENTIALS REQUIRED. Not tested against a real Supabase project yet.
export class SupabaseSessionSummaryRepository implements SessionSummaryRepository {
  async getBySessionId(sessionId: string): Promise<SessionSummary | null> {
    const supabase = getSupabaseServiceClient();
    const { data, error } = await supabase
      .from("session_results")
      .select("*")
      .eq("session_id", sessionId)
      .maybeSingle();
    if (error) throw error;
    return data ? toSummary(data as ResultRow) : null;
  }

  async save(summary: SessionSummary): Promise<SessionSummary> {
    const supabase = getSupabaseServiceClient();
    const { error } = await supabase.from("session_results").upsert(
      {
        session_id: summary.sessionId,
        completed_all_slides: summary.completedAllSlides,
        last_slide_object_id: summary.lastSlideObjectId ?? null,
        questions_summary: summary.questions,
        unanswered_points: summary.unansweredPoints,
      },
      { onConflict: "session_id" },
    );
    if (error) throw error;
    return summary;
  }
}

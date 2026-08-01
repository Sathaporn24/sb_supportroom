import "server-only";
import type { SessionQuestionRepository } from "@/providers/data/repository-types";
import type { AnswerStatus, CreateSessionQuestionInput, SessionQuestion } from "@/types/domain";
import { getSupabaseServiceClient } from "@/providers/data/supabase/client";

type QuestionRow = {
  id: string;
  session_id: string;
  slide_object_id: string | null;
  transcript: string | null;
  answer: string | null;
  answer_status: AnswerStatus;
  created_at: string;
};

function toQuestion(row: QuestionRow): SessionQuestion {
  return {
    id: row.id,
    sessionId: row.session_id,
    slideObjectId: row.slide_object_id ?? undefined,
    transcript: row.transcript ?? undefined,
    answer: row.answer ?? undefined,
    answerStatus: row.answer_status,
    createdAt: row.created_at,
  };
}

// PREPARED — CREDENTIALS REQUIRED. Not tested against a real Supabase project yet.
export class SupabaseSessionQuestionRepository implements SessionQuestionRepository {
  async add(input: CreateSessionQuestionInput): Promise<SessionQuestion> {
    const supabase = getSupabaseServiceClient();
    const { data, error } = await supabase
      .from("session_questions")
      .insert({
        session_id: input.sessionId,
        slide_object_id: input.slideObjectId ?? null,
        transcript: input.transcript ?? null,
        answer: input.answer ?? null,
        answer_status: input.answerStatus,
      })
      .select()
      .single();
    if (error) throw error;
    return toQuestion(data as QuestionRow);
  }

  async listBySession(sessionId: string): Promise<SessionQuestion[]> {
    const supabase = getSupabaseServiceClient();
    const { data, error } = await supabase
      .from("session_questions")
      .select("*")
      .eq("session_id", sessionId)
      .order("created_at");
    if (error) throw error;
    return (data ?? []).map((row) => toQuestion(row as QuestionRow));
  }
}

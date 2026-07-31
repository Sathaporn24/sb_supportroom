import "server-only";
import { getProviderSelection } from "@/config/env";
import type {
  LessonConfigRepository,
  SessionQuestionRepository,
  SessionRepository,
  SessionSummaryRepository,
} from "@/providers/data/repository-types";
import { MockLessonConfigRepository } from "@/providers/data/mock/mock-lesson-config-repository";
import { MockSessionRepository } from "@/providers/data/mock/mock-session-repository";
import { MockSessionQuestionRepository } from "@/providers/data/mock/mock-session-question-repository";
import { MockSessionSummaryRepository } from "@/providers/data/mock/mock-session-summary-repository";
import { resetMockStore } from "@/providers/data/mock/store";
import { SupabaseLessonConfigRepository } from "@/providers/data/supabase/supabase-lesson-config-repository";
import { SupabaseSessionRepository } from "@/providers/data/supabase/supabase-session-repository";
import { SupabaseSessionQuestionRepository } from "@/providers/data/supabase/supabase-session-question-repository";
import { SupabaseSessionSummaryRepository } from "@/providers/data/supabase/supabase-session-summary-repository";

// Single composition point - Route Handlers import these, never a concrete
// implementation directly. Swapping DATA_PROVIDER=supabase changes every repository
// at once with no other code changes required.
function isSupabase(): boolean {
  return getProviderSelection().DATA_PROVIDER === "supabase";
}

export function createLessonConfigRepository(): LessonConfigRepository {
  return isSupabase() ? new SupabaseLessonConfigRepository() : new MockLessonConfigRepository();
}

export function createSessionRepository(): SessionRepository {
  return isSupabase() ? new SupabaseSessionRepository() : new MockSessionRepository();
}

export function createSessionQuestionRepository(): SessionQuestionRepository {
  return isSupabase() ? new SupabaseSessionQuestionRepository() : new MockSessionQuestionRepository();
}

export function createSessionSummaryRepository(): SessionSummaryRepository {
  return isSupabase() ? new SupabaseSessionSummaryRepository() : new MockSessionSummaryRepository();
}

/** Mock-mode only - resets the in-memory store back to seed data. */
export function resetMockData(): void {
  resetMockStore();
}

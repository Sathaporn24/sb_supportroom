import "server-only";
import type { LessonConfig, SessionQuestion, SessionSummary, TrainingSession } from "@/types/domain";
import { seedLessonConfigs } from "@/providers/data/mock/seed";

type MockStore = {
  lessonConfigs: Map<string, LessonConfig>;
  sessions: Map<string, TrainingSession>;
  sessionsByToken: Map<string, string>;
  sessionQuestions: Map<string, SessionQuestion[]>;
  sessionSummaries: Map<string, SessionSummary>;
};

// In-memory only - resets on server restart. This is intentional for Mock Mode; see
// docs/BACKEND_HANDOFF.md for the switch to Supabase. Cached on globalThis so Next.js
// dev-mode hot reload doesn't wipe seeded data on every file save.
declare global {
  var __supportroomMockStore: MockStore | undefined;
}

function createStore(): MockStore {
  const lessonConfigs = new Map<string, LessonConfig>();
  for (const config of seedLessonConfigs()) {
    lessonConfigs.set(config.slug, config);
  }
  return {
    lessonConfigs,
    sessions: new Map(),
    sessionsByToken: new Map(),
    sessionQuestions: new Map(),
    sessionSummaries: new Map(),
  };
}

export function getMockStore(): MockStore {
  if (!globalThis.__supportroomMockStore) {
    globalThis.__supportroomMockStore = createStore();
  }
  return globalThis.__supportroomMockStore;
}

export function resetMockStore(): void {
  globalThis.__supportroomMockStore = createStore();
}

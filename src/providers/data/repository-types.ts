import type {
  CreateSessionInput,
  CreateSessionQuestionInput,
  EndSessionInput,
  LessonConfig,
  SessionQuestion,
  SessionSummary,
  TrainingSession,
} from "@/types/domain";

export interface LessonConfigRepository {
  list(): Promise<LessonConfig[]>;
  getBySlug(slug: string): Promise<LessonConfig | null>;
  save(config: LessonConfig): Promise<LessonConfig>;
}

export interface SessionRepository {
  create(input: CreateSessionInput): Promise<TrainingSession>;
  getByToken(token: string): Promise<TrainingSession | null>;
  getById(id: string): Promise<TrainingSession | null>;
  list(): Promise<TrainingSession[]>;
  markStarted(sessionId: string): Promise<void>;
  end(sessionId: string, result: EndSessionInput): Promise<void>;
}

export interface SessionQuestionRepository {
  add(input: CreateSessionQuestionInput): Promise<SessionQuestion>;
  listBySession(sessionId: string): Promise<SessionQuestion[]>;
}

/** Persists the session_results snapshot created once a session ends. */
export interface SessionSummaryRepository {
  getBySessionId(sessionId: string): Promise<SessionSummary | null>;
  save(summary: SessionSummary): Promise<SessionSummary>;
}

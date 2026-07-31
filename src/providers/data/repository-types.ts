import type { CreateSessionInput, Lesson, SessionSummary, TrainingSession } from "@/types/domain";

export const STORAGE_KEYS = {
  lesson: "supportroom.mock.lesson.v1",
  sessions: "supportroom.mock.sessions.v1",
  reports: "supportroom.mock.reports.v1",
} as const;

export interface LessonRepository {
  getLoginLesson(): Promise<Lesson>;
  saveLoginLesson(lesson: Lesson): Promise<Lesson>;
  resetLoginLesson(): Promise<Lesson>;
}

export interface SessionRepository {
  list(): Promise<TrainingSession[]>;
  create(input: CreateSessionInput): Promise<TrainingSession>;
  getById(id: string): Promise<TrainingSession | null>;
  getByToken(token: string): Promise<TrainingSession | null>;
  update(session: TrainingSession): Promise<TrainingSession>;
  reset(): Promise<void>;
}

export interface ReportRepository {
  getBySessionId(sessionId: string): Promise<SessionSummary | null>;
  save(summary: SessionSummary): Promise<SessionSummary>;
  reset(): Promise<void>;
}

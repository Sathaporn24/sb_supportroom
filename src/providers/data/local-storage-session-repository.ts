import type { CreateSessionInput, TrainingSession } from "@/types/domain";
import { sessionsSeed } from "@/mocks/sessions.mock";
import { readJson, writeJson } from "@/utils/storage";
import { STORAGE_KEYS, type SessionRepository } from "@/providers/data/repository-types";
import { generateId, generatePublicToken } from "@/utils/id";
import { LocalStorageLessonRepository } from "@/providers/data/local-storage-lesson-repository";

function readAll(): TrainingSession[] {
  return readJson<TrainingSession[]>(STORAGE_KEYS.sessions, sessionsSeed);
}

function writeAll(sessions: TrainingSession[]): void {
  writeJson(STORAGE_KEYS.sessions, sessions);
}

export class LocalStorageSessionRepository implements SessionRepository {
  async list(): Promise<TrainingSession[]> {
    return [...readAll()].sort((a, b) => (a.createdAt < b.createdAt ? 1 : -1));
  }

  async create(input: CreateSessionInput): Promise<TrainingSession> {
    const lessonRepository = new LocalStorageLessonRepository();
    const lessonSnapshot = await lessonRepository.getLoginLesson();
    const session: TrainingSession = {
      id: generateId("session"),
      token: generatePublicToken(),
      lessonSnapshot,
      teacherName: input.teacherName?.trim() || undefined,
      schoolName: input.schoolName?.trim() || undefined,
      createdAt: new Date().toISOString(),
      expiresAt: input.expiresAt,
      completedAllSteps: false,
      lastStepIndex: 0,
      lastSegmentIndex: 0,
    };
    const all = readAll();
    all.push(session);
    writeAll(all);
    return session;
  }

  async getById(id: string): Promise<TrainingSession | null> {
    return readAll().find((s) => s.id === id) ?? null;
  }

  async getByToken(token: string): Promise<TrainingSession | null> {
    return readAll().find((s) => s.token === token) ?? null;
  }

  async update(session: TrainingSession): Promise<TrainingSession> {
    const all = readAll();
    const index = all.findIndex((s) => s.id === session.id);
    if (index === -1) {
      all.push(session);
    } else {
      all[index] = session;
    }
    writeAll(all);
    return session;
  }

  async reset(): Promise<void> {
    writeAll([...sessionsSeed]);
  }
}

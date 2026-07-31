import "server-only";
import type { SessionRepository } from "@/providers/data/repository-types";
import type { CreateSessionInput, EndSessionInput, TrainingSession } from "@/types/domain";
import { getMockStore } from "@/providers/data/mock/store";
import { MockLessonConfigRepository } from "@/providers/data/mock/mock-lesson-config-repository";
import { generateId, generatePublicToken } from "@/utils/id";

export class MockSessionRepository implements SessionRepository {
  async create(input: CreateSessionInput): Promise<TrainingSession> {
    const lesson = await new MockLessonConfigRepository().getBySlug(input.lessonSlug);
    if (!lesson) {
      throw new Error(`Unknown lesson slug: ${input.lessonSlug}`);
    }
    const session: TrainingSession = {
      id: generateId("session"),
      token: generatePublicToken(),
      lessonId: lesson.id,
      lessonSlug: lesson.slug,
      teacherName: input.teacherName,
      schoolName: input.schoolName,
      status: "NOT_STARTED",
      createdAt: new Date().toISOString(),
      expiresAt: input.expiresAt,
      completedAllSlides: false,
    };
    const store = getMockStore();
    store.sessions.set(session.id, session);
    store.sessionsByToken.set(session.token, session.id);
    return session;
  }

  async getByToken(token: string): Promise<TrainingSession | null> {
    const store = getMockStore();
    const id = store.sessionsByToken.get(token);
    return id ? (store.sessions.get(id) ?? null) : null;
  }

  async getById(id: string): Promise<TrainingSession | null> {
    return getMockStore().sessions.get(id) ?? null;
  }

  async list(): Promise<TrainingSession[]> {
    return Array.from(getMockStore().sessions.values()).sort((a, b) => (a.createdAt < b.createdAt ? 1 : -1));
  }

  async markStarted(sessionId: string): Promise<void> {
    const store = getMockStore();
    const session = store.sessions.get(sessionId);
    if (!session || session.startedAt) {
      return;
    }
    store.sessions.set(sessionId, { ...session, status: "IN_PROGRESS", startedAt: new Date().toISOString() });
  }

  async end(sessionId: string, result: EndSessionInput): Promise<void> {
    const store = getMockStore();
    const session = store.sessions.get(sessionId);
    if (!session) {
      return;
    }
    store.sessions.set(sessionId, {
      ...session,
      status: "ENDED",
      endedAt: new Date().toISOString(),
      completedAllSlides: result.completedAllSlides,
      lastSlideObjectId: result.lastSlideObjectId ?? session.lastSlideObjectId,
    });
  }
}

import "server-only";
import type { SessionQuestionRepository } from "@/providers/data/repository-types";
import type { CreateSessionQuestionInput, SessionQuestion } from "@/types/domain";
import { getMockStore } from "@/providers/data/mock/store";
import { generateId } from "@/utils/id";

export class MockSessionQuestionRepository implements SessionQuestionRepository {
  async add(input: CreateSessionQuestionInput): Promise<SessionQuestion> {
    const question: SessionQuestion = { ...input, id: generateId("question"), createdAt: new Date().toISOString() };
    const store = getMockStore();
    const list = store.sessionQuestions.get(input.sessionId) ?? [];
    list.push(question);
    store.sessionQuestions.set(input.sessionId, list);
    return question;
  }

  async listBySession(sessionId: string): Promise<SessionQuestion[]> {
    return getMockStore().sessionQuestions.get(sessionId) ?? [];
  }
}

import type { CreateSessionInput, TrainingSession } from "@/types/domain";
import type { SessionRepository } from "@/providers/data/repository-types";

// TODO(Phase 4): implement against a real backend once a database is chosen.
// Must satisfy the same SessionRepository contract so app code does not change.
export class ApiSessionRepository implements SessionRepository {
  async list(): Promise<TrainingSession[]> {
    throw new Error("ApiSessionRepository is not implemented in the mock phase.");
  }

  async create(_input: CreateSessionInput): Promise<TrainingSession> {
    throw new Error("ApiSessionRepository is not implemented in the mock phase.");
  }

  async getById(_id: string): Promise<TrainingSession | null> {
    throw new Error("ApiSessionRepository is not implemented in the mock phase.");
  }

  async getByToken(_token: string): Promise<TrainingSession | null> {
    throw new Error("ApiSessionRepository is not implemented in the mock phase.");
  }

  async update(_session: TrainingSession): Promise<TrainingSession> {
    throw new Error("ApiSessionRepository is not implemented in the mock phase.");
  }

  async reset(): Promise<void> {
    throw new Error("ApiSessionRepository is not implemented in the mock phase.");
  }
}

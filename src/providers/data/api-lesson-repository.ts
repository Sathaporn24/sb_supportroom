import type { Lesson } from "@/types/domain";
import type { LessonRepository } from "@/providers/data/repository-types";

// TODO(Phase 4): implement against a real backend once a database is chosen.
// Must satisfy the same LessonRepository contract so app code does not change.
export class ApiLessonRepository implements LessonRepository {
  async getLoginLesson(): Promise<Lesson> {
    throw new Error("ApiLessonRepository is not implemented in the mock phase.");
  }

  async saveLoginLesson(): Promise<Lesson> {
    throw new Error("ApiLessonRepository is not implemented in the mock phase.");
  }

  async resetLoginLesson(): Promise<Lesson> {
    throw new Error("ApiLessonRepository is not implemented in the mock phase.");
  }
}

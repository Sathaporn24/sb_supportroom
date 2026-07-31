import type { Lesson } from "@/types/domain";
import { loginLessonSeed } from "@/mocks/lesson.mock";
import { readJson, writeJson } from "@/utils/storage";
import { STORAGE_KEYS, type LessonRepository } from "@/providers/data/repository-types";

function cloneSeed(): Lesson {
  return JSON.parse(JSON.stringify(loginLessonSeed)) as Lesson;
}

export class LocalStorageLessonRepository implements LessonRepository {
  async getLoginLesson(): Promise<Lesson> {
    const existing = readJson<Lesson | null>(STORAGE_KEYS.lesson, null);
    if (existing) {
      return existing;
    }
    const seeded = cloneSeed();
    writeJson(STORAGE_KEYS.lesson, seeded);
    return seeded;
  }

  async saveLoginLesson(lesson: Lesson): Promise<Lesson> {
    writeJson(STORAGE_KEYS.lesson, lesson);
    return lesson;
  }

  async resetLoginLesson(): Promise<Lesson> {
    const seeded = cloneSeed();
    writeJson(STORAGE_KEYS.lesson, seeded);
    return seeded;
  }
}

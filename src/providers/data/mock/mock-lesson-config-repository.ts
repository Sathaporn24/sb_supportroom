import "server-only";
import type { LessonConfigRepository } from "@/providers/data/repository-types";
import type { LessonConfig } from "@/types/domain";
import { getMockStore } from "@/providers/data/mock/store";

export class MockLessonConfigRepository implements LessonConfigRepository {
  async list(): Promise<LessonConfig[]> {
    return Array.from(getMockStore().lessonConfigs.values()).sort((a, b) => a.slug.localeCompare(b.slug));
  }

  async getBySlug(slug: string): Promise<LessonConfig | null> {
    return getMockStore().lessonConfigs.get(slug) ?? null;
  }

  async save(config: LessonConfig): Promise<LessonConfig> {
    const updated: LessonConfig = { ...config, updatedAt: new Date().toISOString() };
    getMockStore().lessonConfigs.set(updated.slug, updated);
    return updated;
  }
}

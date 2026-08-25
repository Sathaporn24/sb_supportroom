"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory } from "@/types/domain";
import { LessonForm } from "@/components/admin/LessonForm";

/**
 * P9/Q4 - the UI gap that used to force creating a lesson by hand-firing POST /api/lessons.
 * Deliberately a minimal form (slug/category/title/content source + upload) - slide timing and
 * per-slide video duration stay on the editor page, opened right after this saves (Q4 "scope ขั้นต่ำ").
 * Field markup/validation live in the shared LessonForm - this page only loads the data it needs.
 */
export default function NewLessonPage() {
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [existingSlugs, setExistingSlugs] = useState<string[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    api
      .listKnowledgeCategories()
      .then(({ categories: list }) => setCategories(list))
      .catch((err) => setLoadError(err instanceof ApiClientError ? err.response.error.message : "โหลดรายการหมวดไม่สำเร็จ"));
    api
      .listLessons()
      .then(({ lessons }) => setExistingSlugs(lessons.map((lesson) => lesson.slug)))
      .catch(() => setExistingSlugs([]));
  }, []);

  return <LessonForm mode="create" categories={categories} existingSlugs={existingSlugs} loadError={loadError} />;
}

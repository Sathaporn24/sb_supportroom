"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory, LessonConfig } from "@/types/domain";
import { LessonForm } from "@/components/admin/LessonForm";
import { LoadingBlock } from "@/components/shared/LoadingBlock";

/** Field markup/validation live in the shared LessonForm - this page only loads the lesson and
 * category data, and renders the loading/not-found states before the form has anything to show. */
export default function LessonEditorPage() {
  const params = useParams<{ slug: string }>();
  const [lesson, setLesson] = useState<LessonConfig | null>(null);
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [notFound, setNotFound] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    api
      .listLessons()
      .then(({ lessons }) => {
        const found = lessons.find((l) => l.slug === params.slug);
        if (!found) {
          setNotFound(true);
          return;
        }
        setLesson(found);
      })
      .catch((err) => setLoadError(err instanceof ApiClientError ? err.response.error.message : "โหลดบทเรียนไม่สำเร็จ"));
    api
      .listKnowledgeCategories()
      .then(({ categories: list }) => setCategories(list))
      .catch((err) => setLoadError(err instanceof ApiClientError ? err.response.error.message : "โหลดรายการหมวดไม่สำเร็จ"));
  }, [params.slug]);

  if (notFound) {
    return <main className="p-6 text-muted-foreground">ไม่พบบทเรียนนี้ค่ะ</main>;
  }
  if (loadError && !lesson) {
    return <main className="p-6 text-sm text-destructive">{loadError}</main>;
  }
  if (!lesson) {
    return (
      <main className="p-6">
        <LoadingBlock label="กำลังโหลดบทเรียน..." />
      </main>
    );
  }

  return <LessonForm mode="edit" lesson={lesson} categories={categories} loadError={loadError} />;
}

"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { AdminLink } from "@/components/admin/AdminLink";
import { SlideNarrationEditorCard } from "@/components/admin/SlideNarrationEditorCard";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import { getPdfFilePageNumber } from "@/lib/pdf-slide";
import type { LessonConfig, LessonNarrations } from "@/types/domain";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { CardListSkeleton } from "@/components/shared/CardListSkeleton";
import { LoadingBlock } from "@/components/shared/LoadingBlock";

/**
 * NR-1..NR-9 - per-page narration override editor. PDF-sourced lessons only: Google Slides has
 * no override path at all (NR-9), the server rejects the save even if this screen were reached
 * directly, so the guard below is belt-and-braces, not the only enforcement.
 */
export default function LessonNarrationsPage() {
  const params = useParams<{ slug: string }>();
  const [lesson, setLesson] = useState<LessonConfig | null>(null);
  const [narrations, setNarrations] = useState<LessonNarrations | null>(null);
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [savingId, setSavingId] = useState<string | null>(null);
  const [togglingId, setTogglingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

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
      .catch((err) => setError(err instanceof ApiClientError ? err.response.error.message : "โหลดบทเรียนไม่สำเร็จ"));
  }, [params.slug]);

  useEffect(() => {
    if (!lesson || lesson.contentSourceType !== "pdf") return;
    api
      .getLessonNarrations(lesson.id)
      .then((result) => {
        setNarrations(result);
        setDrafts(Object.fromEntries(result.slides.map((s) => [s.slideObjectId, s.narrationText])));
      })
      .catch((err) => setError(err instanceof ApiClientError ? err.response.error.message : "โหลดบทพูดไม่สำเร็จ"));
  }, [lesson]);

  if (notFound) {
    return <main className="p-6 text-muted-foreground">ไม่พบบทเรียนนี้ค่ะ</main>;
  }
  if (error && !lesson) {
    return <main className="p-6 text-sm text-destructive">{error}</main>;
  }
  if (!lesson) {
    return (
      <main className="p-6">
        <LoadingBlock label="กำลังโหลดบทเรียน..." />
      </main>
    );
  }
  // NR-9 - Google Slides has no narration override path; speaker notes are edited at the source.
  if (lesson.contentSourceType !== "pdf") {
    return (
      <main className="flex w-full flex-col gap-4 p-6">
        <AdminLink
          href={`/admin/lessons/${encodeURIComponent(lesson.slug)}`}
          className="text-xs text-muted-foreground hover:text-foreground"
          data-testid="lesson-narrations-back-link"
        >
          ← กลับหน้าแก้ไขบทเรียน
        </AdminLink>
        <p className="text-sm text-muted-foreground">
          บทเรียนนี้ใช้ Google Slides — แก้บทพูดได้ที่ Speaker Notes ของสไลด์โดยตรง ไม่มีช่องแก้ในระบบนี้
        </p>
      </main>
    );
  }

  async function handleSave(slideObjectId: string) {
    if (!lesson) return;
    setSavingId(slideObjectId);
    setError(null);
    try {
      const text = drafts[slideObjectId] ?? "";
      await api.saveLessonNarration(lesson.id, slideObjectId, text);
      const refreshed = await api.getLessonNarrations(lesson.id);
      setNarrations(refreshed);
      setDrafts(Object.fromEntries(refreshed.slides.map((s) => [s.slideObjectId, s.narrationText])));
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "บันทึกบทพูดไม่สำเร็จ");
    } finally {
      setSavingId(null);
    }
  }

  /** EX-4 - toggle exclusion then reload from the server so isExcluded/lessonIndex stay in sync
   * with what the server just recomputed (renumbering every remaining page - EX-3(ข)). */
  async function handleToggleExcluded(slideObjectId: string, currentlyExcluded: boolean) {
    if (!lesson) return;
    setTogglingId(slideObjectId);
    setError(null);
    try {
      await api.toggleExcludedSlide(lesson.id, slideObjectId, !currentlyExcluded);
      const refreshed = await api.getLessonNarrations(lesson.id);
      setNarrations(refreshed);
      setDrafts(Object.fromEntries(refreshed.slides.map((s) => [s.slideObjectId, s.narrationText])));
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "ตัด/เอาหน้ากลับไม่สำเร็จ");
    } finally {
      setTogglingId(null);
    }
  }

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div>
        <AdminLink
          href={`/admin/lessons/${encodeURIComponent(lesson.slug)}`}
          className="text-xs text-muted-foreground hover:text-foreground"
          data-testid="lesson-narrations-back-link"
        >
          ← กลับหน้าแก้ไขบทเรียน
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold text-primary">บทพูดต่อหน้า: {lesson.title}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          ค่าเริ่มต้นดึงมาจากข้อความในไฟล์ PDF โดยตรง — แก้ทับได้เฉพาะหน้าที่ต้องการ หน้าที่ไม่แตะจะใช้ข้อความจากไฟล์เสมอ
          แก้แล้วบทพูดจะมีผลกับสิ่งที่ AI ใช้ตอบคำถามด้วย (re-index อัตโนมัติ)
        </p>
      </div>

      {narrations?.isLikelyScanned && (
        <Alert variant="destructive">
          <AlertTitle>ไฟล์นี้น่าจะเป็น PDF สแกน</AlertTitle>
          <AlertDescription>
            ทุกหน้าไม่มีข้อความให้ดึงเลย — AI จะไม่มีอะไรพูดถ้าไม่พิมพ์บทพูดเองทุกหน้าในนี้
          </AlertDescription>
        </Alert>
      )}

      {error && <p className="text-sm text-destructive">{error}</p>}

      {!narrations ? (
        <CardListSkeleton count={4} />
      ) : (
        <div className="flex flex-col gap-4">
          {(() => {
            const remainingCount = narrations.slides.filter((s) => !s.isExcluded).length;
            return narrations.slides.map((slide) => {
              const draft = drafts[slide.slideObjectId] ?? "";
              const changed = draft !== slide.narrationText;
              const filePageNumber = getPdfFilePageNumber(slide.slideObjectId);
              const pageLabel = slide.isExcluded
                ? `หน้าที่ ${filePageNumber} ของไฟล์`
                : `หน้า ${(slide.lessonIndex ?? 0) + 1}`;
              return (
                <SlideNarrationEditorCard
                  key={slide.slideObjectId}
                  pageLabel={pageLabel}
                  imageSrc={api.getLessonPdfPageUrl(lesson.pdfDocumentResourceId ?? "", filePageNumber)}
                  value={draft}
                  onChange={(text) => setDrafts((prev) => ({ ...prev, [slide.slideObjectId]: text }))}
                  isExcluded={slide.isExcluded}
                  onToggleExcluded={() => void handleToggleExcluded(slide.slideObjectId, slide.isExcluded)}
                  excludeToggleDisabled={remainingCount <= 1}
                  toggleInFlight={togglingId === slide.slideObjectId}
                  badge={slide.isOverridden && <Badge variant="secondary">แก้ไขแล้ว</Badge>}
                  footer={
                    !slide.isExcluded && (
                      <Button
                        size="sm"
                        onClick={() => handleSave(slide.slideObjectId)}
                        disabled={!changed || savingId === slide.slideObjectId}
                        data-testid={`lesson-narrations-save-button-${slide.slideObjectId}`}
                      >
                        {savingId === slide.slideObjectId ? <Spinner /> : "บันทึก"}
                      </Button>
                    )
                  }
                  testIdPrefix={`lesson-narrations-${slide.slideObjectId}`}
                />
              );
            });
          })()}
        </div>
      )}
    </main>
  );
}

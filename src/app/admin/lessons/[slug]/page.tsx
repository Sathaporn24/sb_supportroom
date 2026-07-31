"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { LessonConfig, SlideConfig } from "@/types/domain";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { formatDateTimeTh } from "@/utils/format";

type FormState = Omit<LessonConfig, "id" | "presentationId" | "createdAt" | "updatedAt">;

function toFormState(lesson: LessonConfig): FormState {
  return {
    slug: lesson.slug,
    title: lesson.title,
    description: lesson.description,
    slidesSourceUrl: lesson.slidesSourceUrl,
    slidesEmbedUrl: lesson.slidesEmbedUrl,
    introWaitMs: lesson.introWaitMs,
    breathPauseMs: lesson.breathPauseMs,
    finalQuestionWaitMs: lesson.finalQuestionWaitMs,
    slideConfigs: lesson.slideConfigs,
    isActive: lesson.isActive,
  };
}

export default function LessonEditorPage() {
  const params = useParams<{ slug: string }>();
  const [lesson, setLesson] = useState<LessonConfig | null>(null);
  const [form, setForm] = useState<FormState | null>(null);
  const [syncStatus, setSyncStatus] = useState<string | null>(null);
  const [syncedAt, setSyncedAt] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [savedAt, setSavedAt] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    void api.listLessons().then(({ lessons }) => {
      const found = lessons.find((l) => l.slug === params.slug);
      if (!found) {
        setNotFound(true);
        return;
      }
      setLesson(found);
      setForm(toFormState(found));
    });
  }, [params.slug]);

  if (notFound) {
    return <main className="p-6 text-room-muted">ไม่พบบทเรียนนี้ค่ะ</main>;
  }
  if (!form) {
    return <main className="p-6 text-room-muted">กำลังโหลด...</main>;
  }

  async function handleSync() {
    if (!form) return;
    setSyncStatus("กำลังตรวจสอบ...");
    try {
      const resolved = await api.resolveSlides({
        slidesSourceUrl: form.slidesSourceUrl,
        slidesEmbedUrl: form.slidesEmbedUrl ?? undefined,
      });
      if (!resolved.presentationId) {
        setSyncStatus(resolved.warning ?? "ไม่สามารถอ่าน presentationId จาก URL นี้ได้");
        return;
      }
      const content = await api.getSlidesContentPreview(resolved.presentationId);
      const existingBySlideId = new Map(form.slideConfigs.map((s) => [s.slideObjectId, s]));
      const nextSlideConfigs: SlideConfig[] = content.slides.map((slide) => ({
        slideObjectId: slide.slideObjectId,
        slideIndex: slide.index,
        videoDurationMs: existingBySlideId.get(slide.slideObjectId)?.videoDurationMs ?? null,
      }));
      setForm({
        ...form,
        title: form.title || content.title,
        slidesEmbedUrl: resolved.embedUrl || form.slidesEmbedUrl,
        slideConfigs: nextSlideConfigs,
      });
      setSyncedAt(content.syncedAt);
      setSyncStatus(resolved.warning ?? `Sync สำเร็จ พบ ${content.slides.length} Slide`);
    } catch (err) {
      setSyncStatus(err instanceof ApiClientError ? err.response.error.message : "Sync ไม่สำเร็จ");
    }
  }

  async function handleSave() {
    if (!form) return;
    setSaving(true);
    try {
      const { lesson: saved } = await api.saveLesson(form);
      setLesson(saved);
      setForm(toFormState(saved));
      setSavedAt(new Date().toLocaleTimeString("th-TH"));
    } catch (err) {
      setSyncStatus(err instanceof ApiClientError ? err.response.error.message : "บันทึกไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  function updateSlideDuration(slideObjectId: string, videoDurationMs: number | null) {
    if (!form) return;
    setForm({
      ...form,
      slideConfigs: form.slideConfigs.map((s) => (s.slideObjectId === slideObjectId ? { ...s, videoDurationMs } : s)),
    });
  }

  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <Link href="/admin/lessons" className="text-xs text-room-muted hover:text-room-text">
            ← กลับรายการบทเรียน
          </Link>
          <h1 className="mt-1 text-xl font-semibold text-room-text">แก้ไขบทเรียน: {form.title}</h1>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="ghost" onClick={handleSync}>
            ตรวจสอบ/Sync Slides
          </Button>
          <Button onClick={handleSave} disabled={saving}>
            บันทึก
          </Button>
        </div>
      </div>

      {savedAt && <p className="text-xs text-room-accent">บันทึกแล้วเมื่อ {savedAt}</p>}
      {syncStatus && <p className="text-xs text-amber-700">{syncStatus}</p>}
      {syncedAt && <p className="text-xs text-room-muted">Sync ล่าสุด: {formatDateTimeTh(syncedAt)}</p>}
      {lesson?.presentationId && !syncedAt && (
        <p className="text-xs text-room-muted">presentationId ปัจจุบัน: {lesson.presentationId}</p>
      )}

      <Card className="space-y-4">
        <label className="block text-sm">
          <span className="mb-1 block text-room-muted">ชื่อบทเรียน</span>
          <input
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
          />
        </label>
        <label className="block text-sm">
          <span className="mb-1 block text-room-muted">Google Slides Source URL (ลิงก์แก้ไข /edit)</span>
          <input
            value={form.slidesSourceUrl}
            onChange={(e) => setForm({ ...form, slidesSourceUrl: e.target.value })}
            placeholder="https://docs.google.com/presentation/d/xxxxx/edit"
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
          />
        </label>
        <label className="block text-sm">
          <span className="mb-1 block text-room-muted">Published/Embed URL (ไม่บังคับ ถ้าไม่ระบุจะสร้างให้อัตโนมัติ)</span>
          <input
            value={form.slidesEmbedUrl ?? ""}
            onChange={(e) => setForm({ ...form, slidesEmbedUrl: e.target.value || null })}
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
          />
        </label>
        <label className="flex items-center gap-2 text-sm text-room-text">
          <input
            type="checkbox"
            checked={form.isActive}
            onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
            className="h-4 w-4 rounded border-room-border"
          />
          เปิดใช้งานบทเรียนนี้ (พร้อมให้สร้างลิงก์การสอน)
        </label>
        <p className="text-xs text-room-muted">
          หมายเหตุ: 1 Slide = 1 ช่วงการสอน · Speaker Notes ของแต่ละ Slide คือบทพูดของ AI โดยตรง ไม่ต้องใส่คำสั่งพิเศษใดๆ ในช่อง
          Notes · หากมีวิดีโอใน Slide ต้องปิดเสียงวิดีโอไว้เสมอ
        </p>
      </Card>

      <Card className="space-y-4">
        <p className="text-xs uppercase tracking-wide text-room-muted">จังหวะเวลา (ทั้งบทเรียน)</p>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <label className="block text-sm">
            <span className="mb-1 block text-room-muted">รอตอบรับก่อนเริ่ม (ms)</span>
            <input
              type="number"
              min={0}
              value={form.introWaitMs}
              onChange={(e) => setForm({ ...form, introWaitMs: Number(e.target.value) })}
              className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block text-room-muted">เว้นจังหวะหายใจระหว่าง Slide (ms)</span>
            <input
              type="number"
              min={0}
              value={form.breathPauseMs}
              onChange={(e) => setForm({ ...form, breathPauseMs: Number(e.target.value) })}
              className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block text-room-muted">รอคำถามท้ายบทเรียน (ms)</span>
            <input
              type="number"
              min={0}
              value={form.finalQuestionWaitMs}
              onChange={(e) => setForm({ ...form, finalQuestionWaitMs: Number(e.target.value) })}
              className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
            />
          </label>
        </div>
      </Card>

      <section className="space-y-3">
        <p className="text-xs uppercase tracking-wide text-room-muted">รายการ Slide ({form.slideConfigs.length})</p>
        {form.slideConfigs.length === 0 && (
          <p className="rounded-xl border border-dashed border-room-border p-6 text-center text-sm text-room-muted">
            ยังไม่มีข้อมูล Slide กด &quot;ตรวจสอบ/Sync Slides&quot; ด้านบนเพื่อดึงรายการ
          </p>
        )}
        {form.slideConfigs.map((slide, index) => (
          <Card key={slide.slideObjectId} className="space-y-2">
            <p className="text-xs font-medium text-room-muted">
              Slide {index + 1} · {slide.slideObjectId}
            </p>
            <label className="flex items-center gap-2 text-sm text-room-text">
              ความยาววิดีโอในสไลด์นี้ (ms, ใส่ 0 ถ้าไม่มีวิดีโอ)
              <input
                type="number"
                min={0}
                value={slide.videoDurationMs ?? 0}
                onChange={(e) => updateSlideDuration(slide.slideObjectId, Number(e.target.value) || null)}
                className="w-32 rounded-lg border border-room-border bg-room-bg px-2 py-1 text-room-text outline-none focus:border-room-accent"
              />
            </label>
          </Card>
        ))}
      </section>

      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={handleSync}>
          ตรวจสอบ/Sync Slides
        </Button>
        <Button onClick={handleSave} disabled={saving}>
          บันทึก
        </Button>
      </div>
    </main>
  );
}

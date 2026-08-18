"use client";

import { useEffect, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { ContentSourceType, LessonConfig, SlideConfig } from "@/types/domain";
import { DocumentUploadList } from "@/components/admin/DocumentUploadList";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Spinner } from "@/components/ui/spinner";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import { formatDateTimeTh } from "@/utils/format";

type FormState = Omit<LessonConfig, "id" | "presentationId" | "createdAt" | "updatedAt">;

function toFormState(lesson: LessonConfig): FormState {
  return {
    slug: lesson.slug,
    title: lesson.title,
    description: lesson.description,
    slidesSourceUrl: lesson.slidesSourceUrl,
    slidesEmbedUrl: lesson.slidesEmbedUrl,
    contentSourceType: lesson.contentSourceType,
    pdfDocumentResourceId: lesson.pdfDocumentResourceId,
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
  const [syncing, setSyncing] = useState(false);
  const [pdfUploading, setPdfUploading] = useState(false);
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
    return <main className="p-6 text-muted-foreground">ไม่พบบทเรียนนี้ค่ะ</main>;
  }
  if (!form) {
    return (
      <main className="p-6">
        <LoadingBlock label="กำลังโหลดบทเรียน..." />
      </main>
    );
  }

  async function handleSync() {
    if (!form) return;
    setSyncing(true);
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
    } finally {
      setSyncing(false);
    }
  }

  async function handlePdfUpload(file: File) {
    if (!form) return;
    setPdfUploading(true);
    setSyncStatus("กำลังอัปโหลด PDF...");
    try {
      const { document } = await api.uploadDocument(file, form.slug);
      const content = await api.previewPdfLessonContent(document.id);
      const existingBySlideId = new Map(form.slideConfigs.map((s) => [s.slideObjectId, s]));
      const nextSlideConfigs: SlideConfig[] = content.slides.map((slide) => ({
        slideObjectId: slide.slideObjectId,
        slideIndex: slide.index,
        videoDurationMs: existingBySlideId.get(slide.slideObjectId)?.videoDurationMs ?? null,
      }));
      setForm({
        ...form,
        title: form.title || content.title,
        pdfDocumentResourceId: document.id,
        slideConfigs: nextSlideConfigs,
      });
      setSyncedAt(content.syncedAt);
      setSyncStatus(`อัปโหลดสำเร็จ พบ ${content.slides.length} หน้า`);
    } catch (err) {
      setSyncStatus(err instanceof ApiClientError ? err.response.error.message : "อัปโหลด PDF ไม่สำเร็จ");
    } finally {
      setPdfUploading(false);
    }
  }

  function handleContentSourceChange(next: ContentSourceType) {
    if (!form) return;
    setForm({
      ...form,
      contentSourceType: next,
      // Each source's own fields don't apply to the other - clear them on switch so a stale
      // Google URL or PDF pointer from before can't linger and confuse a later save.
      ...(next === "pdf"
        ? { slidesSourceUrl: "", slidesEmbedUrl: null }
        : { pdfDocumentResourceId: undefined }),
      slideConfigs: [],
    });
    setSyncStatus(null);
    setSyncedAt(null);
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

  const syncButtonContent = syncing ? (
    <>
      <Spinner data-icon="inline-start" />
      กำลังตรวจสอบ...
    </>
  ) : (
    "ตรวจสอบ/Sync Slides"
  );
  const saveButtonContent = saving ? (
    <>
      <Spinner data-icon="inline-start" />
      กำลังบันทึก...
    </>
  ) : (
    "บันทึก"
  );

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <AdminLink href="/admin/lessons" className="text-xs text-muted-foreground hover:text-foreground">
            ← กลับรายการบทเรียน
          </AdminLink>
          <h1 className="mt-1 text-xl font-semibold">แก้ไขบทเรียน: {form.title}</h1>
        </div>
        <div className="flex items-center gap-2">
          {form.contentSourceType === "google_slides" && (
            <Button variant="ghost" onClick={handleSync} disabled={syncing}>
              {syncButtonContent}
            </Button>
          )}
          <Button onClick={handleSave} disabled={saving}>
            {saveButtonContent}
          </Button>
        </div>
      </div>

      {savedAt && <p className="text-xs text-primary">บันทึกแล้วเมื่อ {savedAt}</p>}
      {/* ข้อความนี้เป็นได้ทั้งสำเร็จและล้มเหลว (เช่น env ที่ยังไม่ได้ตั้ง) - ต้องอ่านออกชัด
          ไม่ใช่สีจางแบบ muted ไม่งั้น error จะกลืนไปกับบรรทัดสถานะอื่น */}
      {syncStatus && <p className="text-xs font-medium text-foreground">{syncStatus}</p>}
      {syncedAt && <p className="text-xs text-muted-foreground">Sync ล่าสุด: {formatDateTimeTh(syncedAt)}</p>}
      {lesson?.presentationId && !syncedAt && (
        <p className="text-xs text-muted-foreground">presentationId ปัจจุบัน: {lesson.presentationId}</p>
      )}

      <Card>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="lesson-title">ชื่อบทเรียน</Label>
            <Input
              id="lesson-title"
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label>แหล่งเนื้อหาสอน</Label>
            <RadioGroup
              value={form.contentSourceType}
              onValueChange={(value) => handleContentSourceChange(value as ContentSourceType)}
              className="flex flex-row gap-4"
            >
              <Label className="font-normal">
                <RadioGroupItem value="google_slides" />
                Google Slides
              </Label>
              <Label className="font-normal">
                <RadioGroupItem value="pdf" />
                PDF
              </Label>
            </RadioGroup>
          </div>

          {form.contentSourceType === "google_slides" ? (
            <>
              <div className="flex flex-col gap-2">
                <Label htmlFor="slides-source-url">Google Slides Source URL (ลิงก์แก้ไข /edit)</Label>
                <Input
                  id="slides-source-url"
                  value={form.slidesSourceUrl}
                  onChange={(e) => setForm({ ...form, slidesSourceUrl: e.target.value })}
                  placeholder="https://docs.google.com/presentation/d/xxxxx/edit"
                />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="slides-embed-url">
                  Published/Embed URL (ไม่บังคับ ถ้าไม่ระบุจะสร้างให้อัตโนมัติ)
                </Label>
                <Input
                  id="slides-embed-url"
                  value={form.slidesEmbedUrl ?? ""}
                  onChange={(e) => setForm({ ...form, slidesEmbedUrl: e.target.value || null })}
                />
              </div>
            </>
          ) : (
            <div className="flex flex-col gap-2">
              <Label htmlFor="pdf-file">ไฟล์ PDF ({form.slideConfigs.length} หน้าที่อ่านได้แล้ว)</Label>
              <Input
                id="pdf-file"
                type="file"
                accept="application/pdf"
                disabled={pdfUploading}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) void handlePdfUpload(file);
                  e.target.value = "";
                }}
                className="h-auto py-1.5"
              />
              <p className="text-xs text-muted-foreground">
                อัปโหลดไฟล์ใหม่เพื่อแทนที่ไฟล์เดิม — แต่ละหน้าจะกลายเป็น 1 Slide
                โดยใช้ข้อความในหน้านั้นเป็นบทพูดของ AI โดยตรง
              </p>
            </div>
          )}

          <Label className="font-normal">
            <Checkbox
              checked={form.isActive}
              onCheckedChange={(checked) => setForm({ ...form, isActive: checked === true })}
            />
            เปิดใช้งานบทเรียนนี้ (พร้อมให้สร้างลิงก์การสอน)
          </Label>
          <p className="text-xs text-muted-foreground">
            หมายเหตุ: 1 Slide = 1 ช่วงการสอน · Speaker Notes ของแต่ละ Slide คือบทพูดของ AI โดยตรง
            ไม่ต้องใส่คำสั่งพิเศษใดๆ ในช่อง Notes · หากมีวิดีโอใน Slide ต้องปิดเสียงวิดีโอไว้เสมอ
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-xs tracking-wide text-muted-foreground uppercase">
            จังหวะเวลา (ทั้งบทเรียน)
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div className="flex flex-col gap-2">
            <Label htmlFor="intro-wait">รอตอบรับก่อนเริ่ม (ms)</Label>
            <Input
              id="intro-wait"
              type="number"
              min={0}
              value={form.introWaitMs}
              onChange={(e) => setForm({ ...form, introWaitMs: Math.max(0, Number(e.target.value) || 0) })}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="breath-pause">เว้นจังหวะหายใจระหว่าง Slide (ms)</Label>
            <Input
              id="breath-pause"
              type="number"
              min={0}
              value={form.breathPauseMs}
              onChange={(e) => setForm({ ...form, breathPauseMs: Math.max(0, Number(e.target.value) || 0) })}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="final-question-wait">รอคำถามท้ายบทเรียน (ms)</Label>
            <Input
              id="final-question-wait"
              type="number"
              min={0}
              value={form.finalQuestionWaitMs}
              onChange={(e) => setForm({ ...form, finalQuestionWaitMs: Math.max(0, Number(e.target.value) || 0) })}
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-xs tracking-wide text-muted-foreground uppercase">เอกสารประกอบ</CardTitle>
          <p className="mt-1 text-xs text-muted-foreground">
            เอกสารในนี้จะถูกใช้ตอบคำถามเฉพาะบทเรียนนี้เท่านั้น — ถ้าต้องการให้ใช้ได้ทุกบทเรียน ให้อัปโหลดที่{" "}
            <AdminLink href="/admin/documents" className="text-primary hover:underline">
              คลังเอกสารกลาง
            </AdminLink>{" "}
            แทน
          </p>
        </CardHeader>
        <CardContent>
          <DocumentUploadList
            lessonSlug={form.slug}
            primaryDocumentId={form.contentSourceType === "pdf" ? form.pdfDocumentResourceId : undefined}
          />
        </CardContent>
      </Card>

      <section className="flex flex-col gap-3">
        <p className="text-xs tracking-wide text-muted-foreground uppercase">
          รายการ Slide ({form.slideConfigs.length})
        </p>
        {form.slideConfigs.length === 0 && (
          <Empty className="border">
            <EmptyHeader>
              <EmptyTitle>ยังไม่มีข้อมูล Slide</EmptyTitle>
              <EmptyDescription>กด &quot;ตรวจสอบ/Sync Slides&quot; ด้านบนเพื่อดึงรายการ</EmptyDescription>
            </EmptyHeader>
          </Empty>
        )}
        {form.slideConfigs.map((slide, index) => (
          <Card key={slide.slideObjectId} size="sm">
            <CardContent className="flex flex-col gap-2">
              <p className="text-xs font-medium text-muted-foreground">
                Slide {index + 1} · {slide.slideObjectId}
              </p>
              <Label htmlFor={`slide-duration-${slide.slideObjectId}`} className="font-normal">
                ความยาววิดีโอในสไลด์นี้ (ms, ใส่ 0 ถ้าไม่มีวิดีโอ)
                <Input
                  id={`slide-duration-${slide.slideObjectId}`}
                  type="number"
                  min={0}
                  value={slide.videoDurationMs ?? 0}
                  onChange={(e) =>
                    updateSlideDuration(slide.slideObjectId, Math.max(0, Number(e.target.value) || 0) || null)
                  }
                  className="w-32"
                />
              </Label>
            </CardContent>
          </Card>
        ))}
      </section>

      <div className="flex justify-end gap-2">
        {form.contentSourceType === "google_slides" && (
          <Button variant="ghost" onClick={handleSync} disabled={syncing}>
            {syncButtonContent}
          </Button>
        )}
        <Button onClick={handleSave} disabled={saving}>
          {saveButtonContent}
        </Button>
      </div>
    </main>
  );
}

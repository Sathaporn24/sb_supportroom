"use client";

import { useEffect, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { ContentSourceType, KnowledgeCategory, LessonConfig, SlideConfig } from "@/types/domain";
import { CategoryMovePreviewDialog } from "@/components/admin/CategoryMovePreviewDialog";
import { DocumentUploadList } from "@/components/admin/DocumentUploadList";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import { formatDateTimeTh } from "@/utils/format";

type FormState = Omit<LessonConfig, "id" | "presentationId" | "createdAt" | "updatedAt">;

function toFormState(lesson: LessonConfig): FormState {
  return {
    slug: lesson.slug,
    categoryId: lesson.categoryId,
    title: lesson.title,
    description: lesson.description,
    slidesSourceUrl: lesson.slidesSourceUrl,
    slidesEmbedUrl: lesson.slidesEmbedUrl,
    contentSourceType: lesson.contentSourceType,
    pdfDocumentResourceId: lesson.pdfDocumentResourceId,
    slideConfigs: lesson.slideConfigs,
    isActive: lesson.isActive,
  };
}

export default function LessonEditorPage() {
  const params = useParams<{ slug: string }>();
  const [lesson, setLesson] = useState<LessonConfig | null>(null);
  const [form, setForm] = useState<FormState | null>(null);
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [syncStatus, setSyncStatus] = useState<string | null>(null);
  const [syncedAt, setSyncedAt] = useState<string | null>(null);
  const [syncing, setSyncing] = useState(false);
  const [pdfUploading, setPdfUploading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [savedAt, setSavedAt] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  // R3.1/TX-10 - set only while the CS is confirming a category change; the general save is
  // held back until that dialog resolves (or is cancelled) instead of moving silently.
  const [pendingCategoryChange, setPendingCategoryChange] = useState(false);
  // NR-3 - set only while confirming a PDF replacement that would delete existing narration
  // overrides; holds the file back until the CS explicitly confirms (or cancels) instead of
  // uploading and silently wiping edited pages.
  const [pendingPdfReplace, setPendingPdfReplace] = useState<{ file: File; narrationCount: number } | null>(null);
  const [pdfReplaceError, setPdfReplaceError] = useState<string | null>(null);
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
        setForm(toFormState(found));
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
  if (loadError && !form) {
    return <main className="p-6 text-sm text-destructive">{loadError}</main>;
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

  /** NR-3 - a replacement upload for a lesson that already has narration overrides must be
   * confirmed first: pdf-page-N ids are position-based, so inserting/removing a page silently
   * shifts every override onto the wrong page. First upload (no lesson yet, or no overrides
   * saved) skips the dialog entirely - there is nothing to lose. */
  async function handlePdfFileSelected(file: File) {
    if (lesson?.contentSourceType === "pdf" && lesson.pdfDocumentResourceId) {
      try {
        const { count } = await api.getLessonNarrationCount(lesson.id);
        if (count > 0) {
          setPdfReplaceError(null);
          setPendingPdfReplace({ file, narrationCount: count });
          return;
        }
      } catch (err) {
        setPdfReplaceError(err instanceof ApiClientError ? err.response.error.message : "ตรวจสอบบทพูดที่แก้ไว้ไม่สำเร็จ");
        return;
      }
    }
    void handlePdfUpload(file);
  }

  async function handlePdfReplaceConfirmed() {
    if (!pendingPdfReplace) return;
    const { file } = pendingPdfReplace;
    setPendingPdfReplace(null);
    await handlePdfUpload(file);
  }

  async function handlePdfUpload(file: File) {
    if (!form) return;
    setPdfUploading(true);
    setSyncStatus("กำลังอัปโหลด PDF...");
    try {
      const { document } = await api.uploadDocument(file, { scopeType: "lesson", scopeId: lesson?.id });
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

  async function doSave() {
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

  /** R3.1/TX-10 - a changed category never reaches the general save silently: it must go through
   * the move-preview confirmation dialog first, which itself calls PUT .../category. */
  function handleSave() {
    if (!form || !lesson) return;
    if (form.categoryId !== lesson.categoryId) {
      setPendingCategoryChange(true);
      return;
    }
    void doSave();
  }

  function handleCategoryMoveConfirmed() {
    setPendingCategoryChange(false);
    // The dialog already committed the category via PUT .../category - lesson.categoryId is
    // stale until the general save below refreshes it, so update it here too to keep the
    // "has this changed?" check in handleSave() correct if the CS opens the dialog again.
    setLesson((prev) => (prev && form ? { ...prev, categoryId: form.categoryId } : prev));
    void doSave();
  }

  function handleCategoryMoveCancelled() {
    setPendingCategoryChange(false);
    if (lesson) {
      setForm((prev) => (prev ? { ...prev, categoryId: lesson.categoryId } : prev));
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

  // TX-4 - a lesson can only belong to a Level 2 (subcategory) row.
  const parentsById = new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c]));
  const subcategories = categories
    .filter((c) => c.level === 2)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));
  const selectedCategoryName = subcategories.find((c) => c.id === form.categoryId)?.name ?? "";

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

      {loadError && <p className="text-sm text-destructive">{loadError}</p>}
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
            <Label htmlFor="lesson-category">หมวด</Label>
            <Select
              value={form.categoryId}
              onValueChange={(value) => value && setForm({ ...form, categoryId: value })}
            >
              <SelectTrigger id="lesson-category" className="w-full">
                <SelectValue placeholder="เลือกหมวด" />
              </SelectTrigger>
              <SelectContent>
                {subcategories.map((sub) => (
                  <SelectItem key={sub.id} value={sub.id}>
                    {parentsById.get(sub.parentId ?? "")?.name ?? "?"} › {sub.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">
              เปลี่ยนหมวดแล้วกด &quot;บันทึก&quot; — ระบบจะแสดงผลกระทบให้ยืนยันก่อนย้ายจริงเสมอ
            </p>
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
                  if (file) void handlePdfFileSelected(file);
                  e.target.value = "";
                }}
                className="h-auto py-1.5"
              />
              <p className="text-xs text-muted-foreground">
                อัปโหลดไฟล์ใหม่เพื่อแทนที่ไฟล์เดิม — แต่ละหน้าจะกลายเป็น 1 Slide
                โดยใช้ข้อความในหน้านั้นเป็นบทพูดของ AI โดยตรง
              </p>
              {pdfReplaceError && <p className="text-xs text-destructive">{pdfReplaceError}</p>}
              {lesson?.pdfDocumentResourceId && lesson.contentSourceType === "pdf" && (
                <AdminLink
                  href={`/admin/lessons/${encodeURIComponent(lesson.slug)}/narrations`}
                  className="text-xs text-primary hover:underline"
                >
                  แก้บทพูดต่อหน้า →
                </AdminLink>
              )}
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
          {lesson && (
            <DocumentUploadList
              fixedScope={{ scopeType: "lesson", scopeId: lesson.id }}
              primaryDocumentId={form.contentSourceType === "pdf" ? form.pdfDocumentResourceId : undefined}
            />
          )}
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

      {lesson && (
        <CategoryMovePreviewDialog
          open={pendingCategoryChange}
          lessonId={lesson.id}
          currentCategoryId={lesson.categoryId}
          targetCategoryId={form.categoryId}
          targetCategoryName={selectedCategoryName}
          onConfirmed={handleCategoryMoveConfirmed}
          onCancel={handleCategoryMoveCancelled}
        />
      )}

      <AlertDialog open={pendingPdfReplace !== null} onOpenChange={(next) => !next && setPendingPdfReplace(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>แทนที่ไฟล์ PDF เดิม?</AlertDialogTitle>
            <AlertDialogDescription>
              บทพูดที่แก้ไว้ {pendingPdfReplace?.narrationCount ?? 0} หน้าจะถูกลบทั้งหมด เพราะเลขหน้าของไฟล์ใหม่อาจไม่ตรงกับไฟล์เดิม
              — ทุกหน้าจะกลับไปใช้ข้อความที่ดึงได้จากไฟล์ใหม่แทน
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>ยกเลิก</AlertDialogCancel>
            <AlertDialogAction onClick={() => void handlePdfReplaceConfirmed()}>ยืนยันแทนที่</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}

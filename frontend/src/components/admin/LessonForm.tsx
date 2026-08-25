"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { InfoIcon, XIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { ContentSourceType, KnowledgeCategory, LessonConfig, LessonConfigInput, SlideConfig } from "@/types/domain";
import { AdminLink } from "@/components/admin/AdminLink";
import { CategoryMovePreviewDialog } from "@/components/admin/CategoryMovePreviewDialog";
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
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { FieldError } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { formatDateTimeTh } from "@/utils/format";

export type LessonFormState = LessonConfigInput;

function slugify(value: string): string {
  return value
    .trim()
    .replace(/[\s_]+/g, "-")
    .toLowerCase()
    .replace(/[^a-z0-9ก-๙-]/g, "")
    .replace(/-+/g, "-")
    .replace(/^-+|-+$/g, "");
}

const emptyForm: LessonFormState = {
  slug: "",
  categoryId: "",
  title: "",
  description: "",
  slidesSourceUrl: "",
  slidesEmbedUrl: null,
  contentSourceType: "google_slides",
  pdfDocumentResourceId: undefined,
  slideConfigs: [],
  isActive: false,
};

function toFormState(lesson: LessonConfig): LessonFormState {
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

type LessonFormProps =
  | {
      mode: "create";
      categories: KnowledgeCategory[];
      existingSlugs: string[];
      loadError?: string | null;
    }
  | {
      mode: "edit";
      categories: KnowledgeCategory[];
      lesson: LessonConfig;
      loadError?: string | null;
    };

/**
 * P9/Q4 unification - create and edit used to be two hand-maintained forms that had already
 * drifted apart in small ways. Both now share this one component (mode="create"/"edit") so a
 * field/validation change only has to happen once; layout differences that are real product
 * differences (slug input only exists pre-save, slide durations/documents only exist post-save)
 * stay as explicit mode branches rather than being papered over.
 */
export function LessonForm(props: LessonFormProps) {
  const isEdit = props.mode === "edit";
  const router = useRouter();

  const [lesson, setLesson] = useState<LessonConfig | null>(isEdit ? props.lesson : null);
  const [form, setForm] = useState<LessonFormState>(isEdit ? toFormState(props.lesson) : emptyForm);

  const [slugTouched, setSlugTouched] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [pdfFileName, setPdfFileName] = useState<string | null>(null);
  const pdfInputRef = useRef<HTMLInputElement>(null);
  const [saving, setSaving] = useState(false);
  const [syncing, setSyncing] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [syncedAt, setSyncedAt] = useState<string | null>(null);
  const [savedAt, setSavedAt] = useState<string | null>(null);

  const [pendingCategoryChange, setPendingCategoryChange] = useState(false);
  const [pendingPdfReplace, setPendingPdfReplace] = useState<{ file: File; narrationCount: number } | null>(null);
  const [pdfReplaceError, setPdfReplaceError] = useState<string | null>(null);

  const categories = props.categories;
  const loadError = props.loadError ?? null;

  useEffect(() => {
    if (props.mode !== "create") return;
    const defaultCategory = categories.find((c) => c.level === 2 && c.isSystemDefault);
    if (defaultCategory) {
      setForm((prev) => (prev.categoryId ? prev : { ...prev, categoryId: defaultCategory.id }));
    }
  }, [props.mode, categories]);

  const slugTaken = useMemo(() => {
    if (props.mode !== "create") return false;
    return form.slug.trim().length > 0 && props.existingSlugs.includes(form.slug.trim());
  }, [props, form.slug]);

  const tid = (suffix: string) => (isEdit ? `lesson-editor-${suffix}` : `lessons-new-${suffix}`);

  function handleContentSourceChange(next: ContentSourceType) {
    setForm((prev) => ({
      ...prev,
      contentSourceType: next,
      // Each source's own fields don't apply to the other - clear them on switch so a stale
      // Google URL or PDF pointer from before can't linger and confuse a later save.
      ...(next === "pdf" ? { slidesSourceUrl: "", slidesEmbedUrl: null } : { pdfDocumentResourceId: undefined }),
      slideConfigs: [],
    }));
    setPdfFileName(null);
    setStatus(null);
    setSyncedAt(null);
  }

  async function handleSync() {
    setSyncing(true);
    setStatus("กำลังตรวจสอบ...");
    try {
      const resolved = await api.resolveSlides({
        slidesSourceUrl: form.slidesSourceUrl,
        slidesEmbedUrl: form.slidesEmbedUrl ?? undefined,
      });
      if (!resolved.presentationId) {
        setStatus(resolved.warning ?? "ไม่สามารถอ่าน presentationId จาก URL นี้ได้");
        return;
      }
      const content = await api.getSlidesContentPreview(resolved.presentationId);
      const existingBySlideId = new Map(form.slideConfigs.map((s) => [s.slideObjectId, s]));
      const nextSlideConfigs: SlideConfig[] = content.slides.map((slide) => ({
        slideObjectId: slide.slideObjectId,
        slideIndex: slide.index,
        videoDurationMs: existingBySlideId.get(slide.slideObjectId)?.videoDurationMs ?? null,
      }));
      setForm((prev) => {
        const title = prev.title || content.title;
        return {
          ...prev,
          title,
          slug: slugTouched ? prev.slug : slugify(title),
          slidesEmbedUrl: resolved.embedUrl || prev.slidesEmbedUrl,
          slideConfigs: nextSlideConfigs,
        };
      });
      setSyncedAt(content.syncedAt);
      setStatus(resolved.warning ?? `Sync สำเร็จ พบ ${content.slides.length} Slide`);
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "Sync ไม่สำเร็จ");
    } finally {
      setSyncing(false);
    }
  }

  async function handlePdfUpload(file: File) {
    setUploading(true);
    setStatus("กำลังอัปโหลด PDF...");
    try {
      // No LessonConfig.Id yet in create mode - the lesson row doesn't exist until Save, so it
      // lands in the company-wide library and pdfDocumentResourceId below is what attaches it.
      const { document } = await api.uploadDocument(
        file,
        isEdit ? { scopeType: "lesson", scopeId: lesson?.id } : { scopeType: "company" },
      );
      const content = await api.previewPdfLessonContent(document.id);
      const existingBySlideId = new Map(form.slideConfigs.map((s) => [s.slideObjectId, s]));
      const nextSlideConfigs: SlideConfig[] = content.slides.map((slide) => ({
        slideObjectId: slide.slideObjectId,
        slideIndex: slide.index,
        videoDurationMs: existingBySlideId.get(slide.slideObjectId)?.videoDurationMs ?? null,
      }));
      setForm((prev) => {
        const title = prev.title || content.title;
        return {
          ...prev,
          title,
          slug: slugTouched ? prev.slug : slugify(title),
          pdfDocumentResourceId: document.id,
          slideConfigs: nextSlideConfigs,
        };
      });
      setPdfFileName(document.fileName);
      setSyncedAt(content.syncedAt);
      setStatus(`อัปโหลดสำเร็จ พบ ${content.slides.length} หน้า`);
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "อัปโหลด PDF ไม่สำเร็จ");
    } finally {
      setUploading(false);
    }
  }

  /** NR-3 - a replacement upload for a lesson that already has narration overrides must be
   * confirmed first: pdf-page-N ids are position-based, so inserting/removing a page silently
   * shifts every override onto the wrong page. First upload (no lesson yet, or no overrides
   * saved) skips the dialog entirely - there is nothing to lose. Create mode never has narration
   * overrides to lose, so it always skips straight to the upload. */
  async function handlePdfFileSelected(file: File) {
    if (isEdit && lesson?.contentSourceType === "pdf" && lesson.pdfDocumentResourceId) {
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

  function handleClearPdf() {
    setForm((prev) => ({
      ...prev,
      pdfDocumentResourceId: undefined,
      slideConfigs: [],
    }));
    setPdfFileName(null);
    if (pdfInputRef.current) {
      pdfInputRef.current.value = "";
    }
  }

  async function handleCreate() {
    setSaving(true);
    setStatus(null);
    try {
      const { lesson: created } = await api.saveLesson(form);
      router.push(`/admin/lessons/${encodeURIComponent(created.slug)}`);
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "สร้างบทเรียนไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  async function doSave() {
    setSaving(true);
    try {
      const { lesson: saved } = await api.saveLesson(form);
      setLesson(saved);
      setForm(toFormState(saved));
      setSavedAt(new Date().toLocaleTimeString("th-TH"));
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "บันทึกไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  /** R3.1/TX-10 - a changed category never reaches the general save silently: it must go through
   * the move-preview confirmation dialog first, which itself calls PUT .../category. */
  function handleSave() {
    if (!lesson) return;
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
    setLesson((prev) => (prev ? { ...prev, categoryId: form.categoryId } : prev));
    void doSave();
  }

  function handleCategoryMoveCancelled() {
    setPendingCategoryChange(false);
    if (lesson) {
      setForm((prev) => ({ ...prev, categoryId: lesson.categoryId }));
    }
  }

  function updateSlideDuration(slideObjectId: string, videoDurationMs: number | null) {
    setForm((prev) => ({
      ...prev,
      slideConfigs: prev.slideConfigs.map((s) => (s.slideObjectId === slideObjectId ? { ...s, videoDurationMs } : s)),
    }));
  }

  function toggleSlideHasVideo(slideObjectId: string, hasVideo: boolean) {
    updateSlideDuration(slideObjectId, hasVideo ? 0 : null);
  }

  const subcategories = categories
    .filter((c) => c.level === 2)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));
  const parentsById = new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c]));
  const selectedCategoryName = subcategories.find((c) => c.id === form.categoryId)?.name ?? "";

  const canCreate =
    !isEdit &&
    form.slug.trim().length > 0 &&
    !slugTaken &&
    form.categoryId.length > 0 &&
    form.title.trim().length > 0 &&
    (form.contentSourceType === "google_slides" || Boolean(form.pdfDocumentResourceId));

  const syncButtonContent = isEdit && syncing ? (
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

  const titleField = (
    <div className="flex flex-col gap-2">
      <Label htmlFor="lesson-title">ชื่อบทเรียน</Label>
      <Input
        id="lesson-title"
        value={form.title}
        onChange={(e) => {
          const title = e.target.value;
          setForm((prev) => ({
            ...prev,
            title,
            slug: !isEdit && !slugTouched ? slugify(title) : prev.slug,
          }));
        }}
        data-testid={tid("title-input")}
      />
    </div>
  );

  const categoryField = (
    <div className="flex flex-col gap-2">
      <Label htmlFor="lesson-category">หมวด</Label>
      <Select value={form.categoryId} onValueChange={(value) => value && setForm((prev) => ({ ...prev, categoryId: value }))}>
        <SelectTrigger id="lesson-category" className="w-full" data-testid={tid("category-select")}>
          <SelectValue placeholder="เลือกหมวด">
            {(value: string) => {
              const sub = subcategories.find((c) => c.id === value);
              return sub ? `${parentsById.get(sub.parentId ?? "")?.name ?? "?"} › ${sub.name}` : "เลือกหมวด";
            }}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          {subcategories.map((sub) => (
            <SelectItem key={sub.id} value={sub.id}>
              {parentsById.get(sub.parentId ?? "")?.name ?? "?"} › {sub.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {isEdit && (
        <p className="text-xs text-muted-foreground">
          เปลี่ยนหมวดแล้วกด &quot;บันทึก&quot; — ระบบจะแสดงผลกระทบให้ยืนยันก่อนย้ายจริงเสมอ
        </p>
      )}
    </div>
  );

  const contentSourceRadio = (
    <div className="flex flex-col gap-2">
      <Label>แหล่งเนื้อหาสอน</Label>
      <RadioGroup
        value={form.contentSourceType}
        onValueChange={(value) => handleContentSourceChange(value as ContentSourceType)}
        className="flex flex-row gap-4"
      >
        <Label className="font-normal">
          <RadioGroupItem value="google_slides" data-testid={tid("source-google-radio")} />
          Google Slides
        </Label>
        <Label className="font-normal">
          <RadioGroupItem value="pdf" data-testid={tid("source-pdf-radio")} />
          PDF
        </Label>
      </RadioGroup>
    </div>
  );

  const pdfField = (
    <div className="flex flex-col gap-2">
      <Label htmlFor="pdf-file">ไฟล์ PDF ({form.slideConfigs.length} หน้าที่อ่านได้แล้ว)</Label>
      {pdfFileName ? (
        <div className="flex h-8 w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm">
          <span className="truncate">{pdfFileName}</span>
          <Button
            type="button"
            variant="ghost"
            size="icon-xs"
            aria-label="ล้างไฟล์ที่แนบ"
            onClick={handleClearPdf}
            data-testid={tid("pdf-clear-button")}
          >
            <XIcon />
          </Button>
        </div>
      ) : (
        <Input
          id="pdf-file"
          ref={pdfInputRef}
          type="file"
          accept="application/pdf"
          disabled={isEdit ? uploading : uploading}
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) void (isEdit ? handlePdfFileSelected(file) : handlePdfUpload(file));
            e.target.value = "";
          }}
          className="h-auto py-1.5"
          data-testid={tid("pdf-file-input")}
        />
      )}
      {uploading && (
        <p className="flex items-center gap-2 text-xs text-muted-foreground">
          <Spinner /> กำลังอัปโหลด...
        </p>
      )}
      {isEdit && (
        <>
          <p className="text-xs text-muted-foreground">
            อัปโหลดไฟล์ใหม่เพื่อแทนที่ไฟล์เดิม — แต่ละหน้าจะกลายเป็น 1 Slide
            โดยใช้ข้อความในหน้านั้นเป็นบทพูดของ AI โดยตรง
          </p>
          {pdfReplaceError && <p className="text-xs text-destructive">{pdfReplaceError}</p>}
          {lesson?.pdfDocumentResourceId && lesson.contentSourceType === "pdf" && (
            <AdminLink
              href={`/admin/lessons/${encodeURIComponent(lesson.slug)}/narrations`}
              className="text-xs text-primary hover:underline"
              data-testid="lesson-editor-narrations-link"
            >
              แก้บทพูดต่อหน้า →
            </AdminLink>
          )}
        </>
      )}
    </div>
  );

  const activeCheckbox = (
    <Label className="font-normal">
      <Checkbox
        checked={form.isActive}
        onCheckedChange={(checked) => setForm((prev) => ({ ...prev, isActive: checked === true }))}
        data-testid={tid("active-checkbox")}
      />
      {isEdit ? "เปิดใช้งานบทเรียนนี้ (พร้อมให้สร้างลิงก์การสอน)" : "เปิดใช้งานบทเรียนนี้ทันที (พร้อมให้สร้างลิงก์การสอน)"}
    </Label>
  );

  if (!isEdit) {
    return (
      <main className="flex w-full flex-col gap-6 p-6">
        <div>
          <h1 className="text-xl font-semibold text-primary">สร้างบทเรียนใหม่</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            กำหนดข้อมูลพื้นฐานให้ครบก่อน — ตั้งเวลาเดินสไลด์และรายละเอียดอื่นๆ ได้ที่หน้าแก้ไขหลังสร้างเสร็จ
          </p>
        </div>

        {status && <p className="text-xs font-medium text-foreground">{status}</p>}
        {loadError && <p className="text-sm text-destructive">{loadError}</p>}

        <Card className="border border-border bg-white ring-0">
          <CardContent className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <div className="flex items-center gap-1.5">
                <Label htmlFor="lesson-slug">รหัสลิงก์บทเรียน</Label>
                <Popover>
                  <PopoverTrigger
                    render={<Button variant="ghost" size="icon-xs" />}
                    aria-label="อธิบายรหัสลิงก์บทเรียน"
                    data-testid="lessons-new-slug-info-button"
                    openOnHover
                    delay={150}
                  >
                    <InfoIcon />
                  </PopoverTrigger>
                  <PopoverContent>
                    รหัสนี้จะกลายเป็นส่วนหนึ่งของลิงก์เข้าสู่หน้าแก้ไขบทเรียน
                    และแยกฐานความรู้ของบทเรียนนี้ออกจากบทเรียนอื่น ห้ามซ้ำกับบทเรียนอื่นในบริษัทเดียวกัน
                  </PopoverContent>
                </Popover>
              </div>
              <Input
                id="lesson-slug"
                value={form.slug}
                onChange={(e) => {
                  setSlugTouched(true);
                  setForm((prev) => ({ ...prev, slug: e.target.value }));
                }}
                placeholder="เช่น attendance-basics"
                aria-invalid={slugTaken}
                data-testid="lessons-new-slug-input"
              />
              {slugTaken && <FieldError>รหัสนี้ถูกใช้แล้ว ลองเปลี่ยนคำอื่น</FieldError>}
            </div>

            {titleField}

            <div className="flex flex-col gap-2">
              <Label htmlFor="lesson-description">คำอธิบาย (ไม่บังคับ)</Label>
              <Input
                id="lesson-description"
                value={form.description ?? ""}
                onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
                data-testid="lessons-new-description-input"
              />
            </div>

            {categoryField}
            {contentSourceRadio}

            {form.contentSourceType === "google_slides" ? (
              <>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="slides-source-url">Google Slides Source URL (ลิงก์แก้ไข /edit)</Label>
                  <Input
                    id="slides-source-url"
                    value={form.slidesSourceUrl}
                    onChange={(e) => setForm((prev) => ({ ...prev, slidesSourceUrl: e.target.value }))}
                    placeholder="https://docs.google.com/presentation/d/xxxxx/edit"
                    data-testid="lessons-new-slides-url-input"
                  />
                </div>
                <Button
                  variant="secondary"
                  onClick={handleSync}
                  disabled={!form.slidesSourceUrl}
                  data-testid="lessons-new-sync-button"
                >
                  ตรวจสอบ/Sync Slides
                </Button>
              </>
            ) : (
              pdfField
            )}

            {activeCheckbox}
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button onClick={handleCreate} disabled={!canCreate || saving} data-testid="lessons-new-submit-button">
            {saving ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังสร้าง...
              </>
            ) : (
              "สร้างบทเรียน"
            )}
          </Button>
        </div>
      </main>
    );
  }

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <AdminLink
            href="/admin/lessons"
            className="text-xs text-muted-foreground hover:text-foreground"
            data-testid="lesson-editor-back-link"
          >
            ← กลับรายการบทเรียน
          </AdminLink>
          <h1 className="mt-1 text-xl font-semibold text-primary">แก้ไขบทเรียน: {form.title}</h1>
        </div>
        <div className="flex items-center gap-2">
          {form.contentSourceType === "google_slides" && (
            <Button variant="ghost" onClick={handleSync} disabled={syncing} data-testid="lesson-editor-sync-button-top">
              {syncButtonContent}
            </Button>
          )}
          <Button onClick={handleSave} disabled={saving} data-testid="lesson-editor-save-button-top">
            {saveButtonContent}
          </Button>
        </div>
      </div>

      {loadError && <p className="text-sm text-destructive">{loadError}</p>}
      {savedAt && <p className="text-xs text-primary">บันทึกแล้วเมื่อ {savedAt}</p>}
      {/* ข้อความนี้เป็นได้ทั้งสำเร็จและล้มเหลว (เช่น env ที่ยังไม่ได้ตั้ง) - ต้องอ่านออกชัด
          ไม่ใช่สีจางแบบ muted ไม่งั้น error จะกลืนไปกับบรรทัดสถานะอื่น */}
      {status && <p className="text-xs font-medium text-foreground">{status}</p>}
      {syncedAt && <p className="text-xs text-muted-foreground">Sync ล่าสุด: {formatDateTimeTh(syncedAt)}</p>}
      {lesson?.presentationId && !syncedAt && (
        <p className="text-xs text-muted-foreground">presentationId ปัจจุบัน: {lesson.presentationId}</p>
      )}

      <Card>
        <CardContent className="flex flex-col gap-4">
          {titleField}
          {categoryField}
          {contentSourceRadio}

          {form.contentSourceType === "google_slides" ? (
            <>
              <div className="flex flex-col gap-2">
                <Label htmlFor="slides-source-url">Google Slides Source URL (ลิงก์แก้ไข /edit)</Label>
                <Input
                  id="slides-source-url"
                  value={form.slidesSourceUrl}
                  onChange={(e) => setForm((prev) => ({ ...prev, slidesSourceUrl: e.target.value }))}
                  placeholder="https://docs.google.com/presentation/d/xxxxx/edit"
                  data-testid="lesson-editor-slides-url-input"
                />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="slides-embed-url">
                  Published/Embed URL (ไม่บังคับ ถ้าไม่ระบุจะสร้างให้อัตโนมัติ)
                </Label>
                <Input
                  id="slides-embed-url"
                  value={form.slidesEmbedUrl ?? ""}
                  onChange={(e) => setForm((prev) => ({ ...prev, slidesEmbedUrl: e.target.value || null }))}
                  data-testid="lesson-editor-slides-embed-input"
                />
              </div>
            </>
          ) : (
            pdfField
          )}

          {activeCheckbox}
          <p className="text-xs text-muted-foreground">
            หมายเหตุ: 1 Slide = 1 ช่วงการสอน · Speaker Notes ของแต่ละ Slide คือบทพูดของ AI โดยตรง
            ไม่ต้องใส่คำสั่งพิเศษใดๆ ในช่อง Notes · หากมีวิดีโอใน Slide ต้องปิดเสียงวิดีโอไว้เสมอ
          </p>
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
              <Label className="font-normal">
                <Checkbox
                  checked={slide.videoDurationMs !== null}
                  onCheckedChange={(checked) => toggleSlideHasVideo(slide.slideObjectId, checked === true)}
                  data-testid={`lesson-editor-slide-has-video-${slide.slideObjectId}`}
                />
                สไลด์นี้มีวิดีโอ
              </Label>
              {slide.videoDurationMs !== null && (
                <Label htmlFor={`slide-duration-${slide.slideObjectId}`} className="font-normal">
                  ความยาววิดีโอ (ms)
                  <Input
                    id={`slide-duration-${slide.slideObjectId}`}
                    type="number"
                    min={0}
                    value={slide.videoDurationMs}
                    onChange={(e) => updateSlideDuration(slide.slideObjectId, Math.max(0, Number(e.target.value) || 0))}
                    className="w-32"
                    data-testid={`lesson-editor-slide-duration-${slide.slideObjectId}`}
                  />
                </Label>
              )}
            </CardContent>
          </Card>
        ))}
      </section>

      <div className="flex justify-end gap-2">
        {form.contentSourceType === "google_slides" && (
          <Button variant="ghost" onClick={handleSync} disabled={syncing} data-testid="lesson-editor-sync-button-bottom">
            {syncButtonContent}
          </Button>
        )}
        <Button onClick={handleSave} disabled={saving} data-testid="lesson-editor-save-button-bottom">
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
        <AlertDialogContent data-testid="lesson-editor-pdf-replace-dialog">
          <AlertDialogHeader>
            <AlertDialogTitle>แทนที่ไฟล์ PDF เดิม?</AlertDialogTitle>
            <AlertDialogDescription>
              บทพูดที่แก้ไว้ {pendingPdfReplace?.narrationCount ?? 0} หน้าจะถูกลบทั้งหมด เพราะเลขหน้าของไฟล์ใหม่อาจไม่ตรงกับไฟล์เดิม
              — ทุกหน้าจะกลับไปใช้ข้อความที่ดึงได้จากไฟล์ใหม่แทน
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel data-testid="lesson-editor-pdf-replace-cancel-button">ยกเลิก</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => void handlePdfReplaceConfirmed()}
              data-testid="lesson-editor-pdf-replace-confirm-button"
            >
              ยืนยันแทนที่
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}

"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { InfoIcon, XIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { ContentSourceType, KnowledgeCategory, LessonConfigInput, SlideConfig } from "@/types/domain";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { FieldError } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";

function slugify(value: string): string {
  return value
    .trim()
    .replace(/[\s_]+/g, "-")
    .toLowerCase()
    .replace(/[^a-z0-9ก-๙-]/g, "")
    .replace(/-+/g, "-")
    .replace(/^-+|-+$/g, "");
}

const emptyForm: LessonConfigInput = {
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

/**
 * P9/Q4 - the UI gap that used to force creating a lesson by hand-firing POST /api/lessons.
 * Deliberately a minimal form (slug/category/title/content source + upload) - slide timing and
 * per-slide video duration stay on the editor page, opened right after this saves (Q4 "scope ขั้นต่ำ").
 */
export default function NewLessonPage() {
  const router = useRouter();
  const [form, setForm] = useState<LessonConfigInput>(emptyForm);
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [existingSlugs, setExistingSlugs] = useState<string[]>([]);
  const [slugTouched, setSlugTouched] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [pdfFileName, setPdfFileName] = useState<string | null>(null);
  const pdfInputRef = useRef<HTMLInputElement>(null);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .listKnowledgeCategories()
      .then(({ categories: list }) => {
        setCategories(list);
        const defaultCategory = list.find((c) => c.level === 2 && c.isSystemDefault);
        if (defaultCategory) {
          setForm((prev) => (prev.categoryId ? prev : { ...prev, categoryId: defaultCategory.id }));
        }
      })
      .catch((err) => setError(err instanceof ApiClientError ? err.response.error.message : "โหลดรายการหมวดไม่สำเร็จ"));
    api
      .listLessons()
      .then(({ lessons }) => setExistingSlugs(lessons.map((lesson) => lesson.slug)))
      .catch(() => setExistingSlugs([]));
  }, []);

  const slugTaken = useMemo(
    () => form.slug.trim().length > 0 && existingSlugs.includes(form.slug.trim()),
    [form.slug, existingSlugs],
  );

  function handleContentSourceChange(next: ContentSourceType) {
    setForm((prev) => ({
      ...prev,
      contentSourceType: next,
      ...(next === "pdf" ? { slidesSourceUrl: "", slidesEmbedUrl: null } : { pdfDocumentResourceId: undefined }),
      slideConfigs: [],
    }));
    setPdfFileName(null);
    setStatus(null);
  }

  async function handleSync() {
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
      const nextSlideConfigs: SlideConfig[] = content.slides.map((slide) => ({
        slideObjectId: slide.slideObjectId,
        slideIndex: slide.index,
        videoDurationMs: null,
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
      setStatus(resolved.warning ?? `Sync สำเร็จ พบ ${content.slides.length} Slide`);
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "Sync ไม่สำเร็จ");
    }
  }

  async function handlePdfUpload(file: File) {
    setUploading(true);
    setStatus("กำลังอัปโหลด PDF...");
    try {
      // No LessonConfig.Id yet - the lesson row doesn't exist until Save, so this lands in the
      // company-wide library and pdfDocumentResourceId below is what actually attaches it.
      const { document } = await api.uploadDocument(file, { scopeType: "company" });
      const content = await api.previewPdfLessonContent(document.id);
      const nextSlideConfigs: SlideConfig[] = content.slides.map((slide) => ({
        slideObjectId: slide.slideObjectId,
        slideIndex: slide.index,
        videoDurationMs: null,
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
      setStatus(`อัปโหลดสำเร็จ พบ ${content.slides.length} หน้า`);
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "อัปโหลด PDF ไม่สำเร็จ");
    } finally {
      setUploading(false);
    }
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
      const { lesson } = await api.saveLesson(form);
      router.push(`/admin/lessons/${encodeURIComponent(lesson.slug)}`);
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "สร้างบทเรียนไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  const subcategories = categories
    .filter((c) => c.level === 2)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));
  const parentsById = new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c]));

  const canCreate =
    form.slug.trim().length > 0 &&
    !slugTaken &&
    form.categoryId.length > 0 &&
    form.title.trim().length > 0 &&
    (form.contentSourceType === "google_slides" || Boolean(form.pdfDocumentResourceId));

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-primary">สร้างบทเรียนใหม่</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          กำหนดข้อมูลพื้นฐานให้ครบก่อน — ตั้งเวลาเดินสไลด์และรายละเอียดอื่นๆ ได้ที่หน้าแก้ไขหลังสร้างเสร็จ
        </p>
      </div>

      {status && <p className="text-xs font-medium text-foreground">{status}</p>}
      {error && <p className="text-sm text-destructive">{error}</p>}

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
                setForm({ ...form, slug: e.target.value });
              }}
              placeholder="เช่น attendance-basics"
              aria-invalid={slugTaken}
              data-testid="lessons-new-slug-input"
            />
            {slugTaken && <FieldError>รหัสนี้ถูกใช้แล้ว ลองเปลี่ยนคำอื่น</FieldError>}
          </div>

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
                  slug: slugTouched ? prev.slug : slugify(title),
                }));
              }}
              data-testid="lessons-new-title-input"
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="lesson-description">คำอธิบาย (ไม่บังคับ)</Label>
            <Input
              id="lesson-description"
              value={form.description ?? ""}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              data-testid="lessons-new-description-input"
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="lesson-category">หมวด</Label>
            <Select value={form.categoryId} onValueChange={(value) => value && setForm({ ...form, categoryId: value })}>
              <SelectTrigger id="lesson-category" className="w-full" data-testid="lessons-new-category-select">
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
          </div>

          <div className="flex flex-col gap-2">
            <Label>แหล่งเนื้อหาสอน</Label>
            <RadioGroup
              value={form.contentSourceType}
              onValueChange={(value) => handleContentSourceChange(value as ContentSourceType)}
              className="flex flex-row gap-4"
            >
              <Label className="font-normal">
                <RadioGroupItem value="google_slides" data-testid="lessons-new-source-google-radio" />
                Google Slides
              </Label>
              <Label className="font-normal">
                <RadioGroupItem value="pdf" data-testid="lessons-new-source-pdf-radio" />
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
                    data-testid="lessons-new-pdf-clear-button"
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
                  disabled={uploading}
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    if (file) void handlePdfUpload(file);
                    e.target.value = "";
                  }}
                  className="h-auto py-1.5"
                  data-testid="lessons-new-pdf-file-input"
                />
              )}
              {uploading && (
                <p className="flex items-center gap-2 text-xs text-muted-foreground">
                  <Spinner /> กำลังอัปโหลด...
                </p>
              )}
            </div>
          )}

          <Label className="font-normal">
            <Checkbox
              checked={form.isActive}
              onCheckedChange={(checked) => setForm({ ...form, isActive: checked === true })}
              data-testid="lessons-new-active-checkbox"
            />
            เปิดใช้งานบทเรียนนี้ทันที (พร้อมให้สร้างลิงก์การสอน)
          </Label>
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

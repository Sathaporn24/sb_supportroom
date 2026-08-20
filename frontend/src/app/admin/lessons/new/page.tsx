"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AdminLink } from "@/components/admin/AdminLink";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { ContentSourceType, KnowledgeCategory, LessonConfigInput, SlideConfig } from "@/types/domain";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";

const emptyForm: LessonConfigInput = {
  slug: "",
  categoryId: "",
  title: "",
  description: "",
  slidesSourceUrl: "",
  slidesEmbedUrl: null,
  contentSourceType: "google_slides",
  pdfDocumentResourceId: undefined,
  introWaitMs: 3000,
  breathPauseMs: 800,
  finalQuestionWaitMs: 5000,
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
  const [uploading, setUploading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState<string | null>(null);

  useEffect(() => {
    void api.listKnowledgeCategories().then(({ categories: list }) => setCategories(list));
  }, []);

  function handleContentSourceChange(next: ContentSourceType) {
    setForm((prev) => ({
      ...prev,
      contentSourceType: next,
      ...(next === "pdf" ? { slidesSourceUrl: "", slidesEmbedUrl: null } : { pdfDocumentResourceId: undefined }),
      slideConfigs: [],
    }));
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
      setForm((prev) => ({
        ...prev,
        title: prev.title || content.title,
        slidesEmbedUrl: resolved.embedUrl || prev.slidesEmbedUrl,
        slideConfigs: nextSlideConfigs,
      }));
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
      setForm((prev) => ({
        ...prev,
        title: prev.title || content.title,
        pdfDocumentResourceId: document.id,
        slideConfigs: nextSlideConfigs,
      }));
      setStatus(`อัปโหลดสำเร็จ พบ ${content.slides.length} หน้า`);
    } catch (err) {
      setStatus(err instanceof ApiClientError ? err.response.error.message : "อัปโหลด PDF ไม่สำเร็จ");
    } finally {
      setUploading(false);
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
    form.categoryId.length > 0 &&
    form.title.trim().length > 0 &&
    (form.contentSourceType === "google_slides" || Boolean(form.pdfDocumentResourceId));

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <AdminLink href="/admin/lessons" className="text-xs text-muted-foreground hover:text-foreground">
          ← กลับรายการบทเรียน
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold">สร้างบทเรียนใหม่</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          กำหนดข้อมูลพื้นฐานให้ครบก่อน — ตั้งเวลาเดินสไลด์และรายละเอียดอื่นๆ ได้ที่หน้าแก้ไขหลังสร้างเสร็จ
        </p>
      </div>

      {status && <p className="text-xs font-medium text-foreground">{status}</p>}

      <Card>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="lesson-slug">Slug (ใช้ในลิงก์ ห้ามซ้ำในบริษัทเดียวกัน)</Label>
            <Input
              id="lesson-slug"
              value={form.slug}
              onChange={(e) => setForm({ ...form, slug: e.target.value })}
              placeholder="เช่น attendance-basics"
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="lesson-title">ชื่อบทเรียน</Label>
            <Input id="lesson-title" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} />
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="lesson-description">คำอธิบาย (ไม่บังคับ)</Label>
            <Input
              id="lesson-description"
              value={form.description ?? ""}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="lesson-category">หมวด</Label>
            <Select value={form.categoryId} onValueChange={(value) => value && setForm({ ...form, categoryId: value })}>
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
              <Button variant="secondary" onClick={handleSync} disabled={!form.slidesSourceUrl}>
                ตรวจสอบ/Sync Slides
              </Button>
            </>
          ) : (
            <div className="flex flex-col gap-2">
              <Label htmlFor="pdf-file">ไฟล์ PDF ({form.slideConfigs.length} หน้าที่อ่านได้แล้ว)</Label>
              <Input
                id="pdf-file"
                type="file"
                accept="application/pdf"
                disabled={uploading}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) void handlePdfUpload(file);
                  e.target.value = "";
                }}
                className="h-auto py-1.5"
              />
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
            />
            เปิดใช้งานบทเรียนนี้ทันที (พร้อมให้สร้างลิงก์การสอน)
          </Label>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button onClick={handleCreate} disabled={!canCreate || saving}>
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

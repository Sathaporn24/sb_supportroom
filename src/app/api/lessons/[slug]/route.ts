import { NextRequest, NextResponse } from "next/server";
import { createLessonConfigRepository } from "@/providers/data";
import { createSlidesContentProvider } from "@/providers/slides";
import { jsonError } from "@/lib/api-response";
import type { TeachingSlide } from "@/types/domain";

export async function GET(_request: NextRequest, { params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;

  const lessonRepo = createLessonConfigRepository();
  const lesson = await lessonRepo.getBySlug(slug);
  if (!lesson || !lesson.isActive) {
    return jsonError("NOT_FOUND", "ไม่พบบทเรียนนี้ หรือยังไม่เปิดใช้งาน", 404);
  }
  if (!lesson.presentationId) {
    return jsonError("CONFIG_ERROR", "บทเรียนนี้ยังไม่ได้ตั้งค่า Google Slides", 409);
  }

  try {
    const slidesProvider = createSlidesContentProvider();
    const content = await slidesProvider.getLessonContent({ presentationId: lesson.presentationId });
    const durationBySlide = new Map(lesson.slideConfigs.map((slide) => [slide.slideObjectId, slide.videoDurationMs ?? 0]));

    const slides: TeachingSlide[] = content.slides
      .slice()
      .sort((a, b) => a.index - b.index)
      .map((slide) => ({ ...slide, videoDurationMs: durationBySlide.get(slide.slideObjectId) ?? 0 }));

    return NextResponse.json({ lesson, embedUrl: content.embedUrl || lesson.slidesEmbedUrl || "", slides });
  } catch (err) {
    return jsonError("UPSTREAM_ERROR", err instanceof Error ? err.message : "ไม่สามารถโหลดเนื้อหาบทเรียนได้", 502);
  }
}

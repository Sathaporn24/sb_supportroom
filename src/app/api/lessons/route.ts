import { NextRequest, NextResponse } from "next/server";
import { z } from "zod";
import { createLessonConfigRepository } from "@/providers/data";
import { createSlidesContentProvider } from "@/providers/slides";
import { jsonError } from "@/lib/api-response";
import type { LessonConfig } from "@/types/domain";

export async function GET() {
  const repo = createLessonConfigRepository();
  const lessons = await repo.list();
  return NextResponse.json({ lessons });
}

const slideConfigSchema = z.object({
  slideObjectId: z.string(),
  slideIndex: z.number().int().min(0),
  videoDurationMs: z.number().int().min(0).nullable(),
});

const bodySchema = z.object({
  slug: z.string().min(1),
  title: z.string().min(1),
  description: z.string().optional(),
  slidesSourceUrl: z.string(),
  slidesEmbedUrl: z.string().optional().nullable(),
  introWaitMs: z.number().int().min(0),
  breathPauseMs: z.number().int().min(0),
  finalQuestionWaitMs: z.number().int().min(0),
  slideConfigs: z.array(slideConfigSchema),
  isActive: z.boolean(),
});

export async function POST(request: NextRequest) {
  const json = await request.json().catch(() => null);
  const parsed = bodySchema.safeParse(json);
  if (!parsed.success) {
    return jsonError("VALIDATION_ERROR", "ข้อมูลบทเรียนไม่ถูกต้อง", 400, parsed.error.flatten());
  }

  const repo = createLessonConfigRepository();
  const existing = await repo.getBySlug(parsed.data.slug);

  // Re-resolve presentationId from the source URL server-side so saving never keeps a
  // stale/mismatched id. Sync failures here don't block saving - CS uses the dedicated
  // "Validate/Sync" button (calls /api/slides/resolve) to see the real error.
  let presentationId = existing?.presentationId ?? null;
  if (parsed.data.slidesSourceUrl) {
    try {
      const slidesProvider = createSlidesContentProvider();
      const resolved = await slidesProvider.resolvePresentation({
        slidesSourceUrl: parsed.data.slidesSourceUrl,
        slidesEmbedUrl: parsed.data.slidesEmbedUrl ?? undefined,
      });
      presentationId = resolved.presentationId;
    } catch {
      // keep previous presentationId
    }
  }

  const now = new Date().toISOString();
  const config: LessonConfig = {
    id: existing?.id ?? crypto.randomUUID(),
    slug: parsed.data.slug,
    title: parsed.data.title,
    description: parsed.data.description,
    slidesSourceUrl: parsed.data.slidesSourceUrl,
    presentationId,
    slidesEmbedUrl: parsed.data.slidesEmbedUrl ?? null,
    introWaitMs: parsed.data.introWaitMs,
    breathPauseMs: parsed.data.breathPauseMs,
    finalQuestionWaitMs: parsed.data.finalQuestionWaitMs,
    slideConfigs: parsed.data.slideConfigs,
    isActive: parsed.data.isActive,
    createdAt: existing?.createdAt ?? now,
    updatedAt: now,
  };

  const saved = await repo.save(config);
  return NextResponse.json({ lesson: saved });
}

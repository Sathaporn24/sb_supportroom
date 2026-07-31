import { NextRequest, NextResponse } from "next/server";
import { z } from "zod";
import {
  createLessonConfigRepository,
  createSessionQuestionRepository,
  createSessionRepository,
  createSessionSummaryRepository,
} from "@/providers/data";
import { jsonError } from "@/lib/api-response";

export async function GET(_request: NextRequest, { params }: { params: Promise<{ token: string }> }) {
  const { token } = await params;
  const repo = createSessionRepository();
  const session = await repo.getByToken(token);
  if (!session) {
    return jsonError("NOT_FOUND", "ไม่พบ Session นี้ หรือลิงก์หมดอายุ", 404);
  }
  const lesson = await createLessonConfigRepository().getBySlug(session.lessonSlug);
  return NextResponse.json({ session, lessonTitle: lesson?.title ?? session.lessonSlug });
}

const patchSchema = z.discriminatedUnion("action", [
  z.object({ action: z.literal("start") }),
  z.object({
    action: z.literal("end"),
    completedAllSlides: z.boolean(),
    lastSlideObjectId: z.string().optional(),
  }),
]);

export async function PATCH(request: NextRequest, { params }: { params: Promise<{ token: string }> }) {
  const { token } = await params;
  const json = await request.json().catch(() => null);
  const parsed = patchSchema.safeParse(json);
  if (!parsed.success) {
    return jsonError("VALIDATION_ERROR", "ข้อมูลไม่ถูกต้อง", 400, parsed.error.flatten());
  }

  const sessionRepo = createSessionRepository();
  const session = await sessionRepo.getByToken(token);
  if (!session) {
    return jsonError("NOT_FOUND", "ไม่พบ Session นี้", 404);
  }

  if (parsed.data.action === "start") {
    await sessionRepo.markStarted(session.id);
  } else {
    await sessionRepo.end(session.id, {
      completedAllSlides: parsed.data.completedAllSlides,
      lastSlideObjectId: parsed.data.lastSlideObjectId,
    });

    const questionRepo = createSessionQuestionRepository();
    const questions = await questionRepo.listBySession(session.id);
    const summaryRepo = createSessionSummaryRepository();
    await summaryRepo.save({
      sessionId: session.id,
      completedAllSlides: parsed.data.completedAllSlides,
      lastSlideObjectId: parsed.data.lastSlideObjectId,
      questions,
      unansweredPoints: questions
        .filter((q) => q.answerStatus === "not_found")
        .map((q) => q.transcript || q.answer || "")
        .filter(Boolean),
      createdAt: new Date().toISOString(),
    });
  }

  const updated = await sessionRepo.getByToken(token);
  return NextResponse.json({ session: updated });
}

import { NextRequest, NextResponse } from "next/server";
import { createVoiceQuestionProvider } from "@/providers/voice-question";
import { createSlidesContentProvider } from "@/providers/slides";
import { createLessonConfigRepository, createSessionQuestionRepository } from "@/providers/data";
import { jsonError } from "@/lib/api-response";
import { uploadLimits } from "@/config/env";

export async function POST(request: NextRequest) {
  const formData = await request.formData().catch(() => null);
  if (!formData) {
    return jsonError("VALIDATION_ERROR", "ต้องส่งข้อมูลแบบ multipart/form-data", 400);
  }

  const audioFile = formData.get("audio");
  const lessonSlug = formData.get("lessonSlug");
  const sessionId = formData.get("sessionId");
  const currentSlideObjectId = formData.get("currentSlideObjectId");
  const durationMsRaw = formData.get("durationMs");

  if (!(audioFile instanceof File) || typeof lessonSlug !== "string" || typeof sessionId !== "string") {
    return jsonError("VALIDATION_ERROR", "ต้องแนบไฟล์เสียง (audio), lessonSlug และ sessionId", 400);
  }

  const maxBytes = uploadLimits.maxVoiceUploadMb * 1024 * 1024;
  if (audioFile.size > maxBytes) {
    return jsonError("VALIDATION_ERROR", `ไฟล์เสียงใหญ่เกินกำหนด (สูงสุด ${uploadLimits.maxVoiceUploadMb}MB)`, 400);
  }
  if (!audioFile.type.startsWith("audio/") && audioFile.type !== "video/webm") {
    return jsonError("VALIDATION_ERROR", "ชนิดไฟล์ไม่ใช่เสียงที่รองรับ", 400);
  }

  const durationMs = Number(durationMsRaw ?? 0);

  try {
    const lessonRepo = createLessonConfigRepository();
    const lesson = await lessonRepo.getBySlug(lessonSlug);
    if (!lesson || !lesson.presentationId) {
      return jsonError("NOT_FOUND", "ไม่พบบทเรียนหรือยังไม่ได้ตั้งค่า Slides", 404);
    }

    const slidesProvider = createSlidesContentProvider();
    const content = await slidesProvider.getLessonContent({ presentationId: lesson.presentationId });

    const audioBuffer = Buffer.from(await audioFile.arrayBuffer());
    const voiceProvider = createVoiceQuestionProvider();
    const result = await voiceProvider.transcribeAndAnswer({
      audio: audioBuffer,
      mimeType: audioFile.type || "audio/webm",
      durationMs,
      lessonSlides: content.slides.map((slide) => ({
        slideObjectId: slide.slideObjectId,
        speakerNotes: slide.speakerNotes,
      })),
      currentSlideObjectId: typeof currentSlideObjectId === "string" ? currentSlideObjectId : undefined,
    });

    if (result.answerStatus !== "no_speech") {
      const questionRepo = createSessionQuestionRepository();
      await questionRepo.add({
        sessionId,
        slideObjectId:
          result.relatedSlideObjectId ?? (typeof currentSlideObjectId === "string" ? currentSlideObjectId : undefined),
        transcript: result.transcript || undefined,
        answer: result.answer || undefined,
        answerStatus: result.answerStatus,
      });
    }

    return NextResponse.json(result);
  } catch (err) {
    return jsonError("UPSTREAM_ERROR", err instanceof Error ? err.message : "ประมวลผลคำถามไม่สำเร็จ", 502);
  }
}

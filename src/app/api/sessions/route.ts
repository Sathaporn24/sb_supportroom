import { NextRequest, NextResponse } from "next/server";
import { z } from "zod";
import { createSessionRepository } from "@/providers/data";
import { jsonError } from "@/lib/api-response";
import { getDefaultSessionExpiryHours } from "@/config/server-defaults";
import { addHours } from "@/utils/format";

export async function GET() {
  const repo = createSessionRepository();
  const sessions = await repo.list();
  return NextResponse.json({ sessions });
}

const bodySchema = z.object({
  lessonSlug: z.string().min(1),
  teacherName: z.string().optional(),
  schoolName: z.string().optional(),
  expiresAt: z.string().datetime().optional(),
});

export async function POST(request: NextRequest) {
  const json = await request.json().catch(() => null);
  const parsed = bodySchema.safeParse(json);
  if (!parsed.success) {
    return jsonError("VALIDATION_ERROR", "ข้อมูลสำหรับสร้าง Session ไม่ถูกต้อง", 400, parsed.error.flatten());
  }

  const expiresAt = parsed.data.expiresAt ?? addHours(new Date().toISOString(), getDefaultSessionExpiryHours());

  try {
    const repo = createSessionRepository();
    const session = await repo.create({
      lessonSlug: parsed.data.lessonSlug,
      teacherName: parsed.data.teacherName,
      schoolName: parsed.data.schoolName,
      expiresAt,
    });
    return NextResponse.json({ session }, { status: 201 });
  } catch (err) {
    return jsonError("NOT_FOUND", err instanceof Error ? err.message : "ไม่สามารถสร้าง Session ได้", 404);
  }
}

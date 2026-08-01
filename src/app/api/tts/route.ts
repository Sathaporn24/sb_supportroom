import { NextRequest, NextResponse } from "next/server";
import { z } from "zod";
import { createTextToSpeechProvider } from "@/providers/tts";
import { jsonError } from "@/lib/api-response";

const bodySchema = z.object({
  text: z.string().min(1).max(2000),
  voice: z.string().optional(),
  // Pinned to an SSML percentage so a caller can't smuggle arbitrary prosody markup in.
  rate: z
    .string()
    .regex(/^[+-]\d{1,2}%$/)
    .optional(),
});

export async function POST(request: NextRequest) {
  const json = await request.json().catch(() => null);
  const parsed = bodySchema.safeParse(json);
  if (!parsed.success) {
    return jsonError("VALIDATION_ERROR", "ข้อความสำหรับแปลงเสียงไม่ถูกต้องหรือยาวเกินไป", 400, parsed.error.flatten());
  }

  try {
    const provider = createTextToSpeechProvider();
    const result = await provider.synthesize(parsed.data);
    return new NextResponse(new Blob([new Uint8Array(result.audio)], { type: result.mimeType }), {
      headers: { "Content-Type": result.mimeType, "Cache-Control": "no-store" },
    });
  } catch (err) {
    // Never log the full speaker-notes/answer text here - message only.
    return jsonError("UPSTREAM_ERROR", err instanceof Error ? err.message : "แปลงข้อความเป็นเสียงไม่สำเร็จ", 502);
  }
}

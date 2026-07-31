import { NextRequest, NextResponse } from "next/server";
import { z } from "zod";
import { createSlidesContentProvider } from "@/providers/slides";
import { jsonError } from "@/lib/api-response";

const bodySchema = z.object({
  slidesSourceUrl: z.string().min(1),
  slidesEmbedUrl: z.string().optional(),
});

export async function POST(request: NextRequest) {
  const json = await request.json().catch(() => null);
  const parsed = bodySchema.safeParse(json);
  if (!parsed.success) {
    return jsonError("VALIDATION_ERROR", "รูปแบบข้อมูลไม่ถูกต้อง", 400, parsed.error.flatten());
  }

  try {
    const provider = createSlidesContentProvider();
    const resolved = await provider.resolvePresentation(parsed.data);
    return NextResponse.json(resolved);
  } catch (err) {
    return jsonError("UPSTREAM_ERROR", err instanceof Error ? err.message : "ไม่สามารถตรวจสอบ Google Slides ได้", 502);
  }
}

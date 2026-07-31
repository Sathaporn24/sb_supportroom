import { NextRequest, NextResponse } from "next/server";
import { createSlidesContentProvider } from "@/providers/slides";
import { jsonError } from "@/lib/api-response";

export async function GET(request: NextRequest) {
  const presentationId = request.nextUrl.searchParams.get("presentationId");
  if (!presentationId) {
    return jsonError("VALIDATION_ERROR", "ต้องระบุ presentationId", 400);
  }

  try {
    const provider = createSlidesContentProvider();
    const content = await provider.getLessonContent({ presentationId });
    return NextResponse.json(content);
  } catch (err) {
    return jsonError("UPSTREAM_ERROR", err instanceof Error ? err.message : "ไม่สามารถอ่านเนื้อหา Slides ได้", 502);
  }
}

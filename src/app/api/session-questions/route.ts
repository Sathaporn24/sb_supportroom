import { NextRequest, NextResponse } from "next/server";
import { createSessionQuestionRepository } from "@/providers/data";
import { jsonError } from "@/lib/api-response";

// Writes happen inside /api/voice-question (the only place a question is actually
// asked+answered) - this route is read-only, used by the Admin summary view.
export async function GET(request: NextRequest) {
  const sessionId = request.nextUrl.searchParams.get("sessionId");
  if (!sessionId) {
    return jsonError("VALIDATION_ERROR", "ต้องระบุ sessionId", 400);
  }
  const repo = createSessionQuestionRepository();
  const questions = await repo.listBySession(sessionId);
  return NextResponse.json({ questions });
}

import { NextRequest, NextResponse } from "next/server";
import { createSessionRepository, createSessionSummaryRepository } from "@/providers/data";
import { jsonError } from "@/lib/api-response";

export async function GET(_request: NextRequest, { params }: { params: Promise<{ token: string }> }) {
  const { token } = await params;
  const sessionRepo = createSessionRepository();
  const session = await sessionRepo.getByToken(token);
  if (!session) {
    return jsonError("NOT_FOUND", "ไม่พบ Session นี้", 404);
  }
  const summaryRepo = createSessionSummaryRepository();
  const summary = await summaryRepo.getBySessionId(session.id);
  return NextResponse.json({ session, summary });
}

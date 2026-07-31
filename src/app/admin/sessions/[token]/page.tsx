"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { getSessionStatus, sessionStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import type { SessionQuestion, SessionSummary, TrainingSession } from "@/types/domain";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";
import { LoadingBlock } from "@/components/ui/LoadingBlock";

const answerStatusLabel: Record<SessionQuestion["answerStatus"], string> = {
  answered: "ตอบแล้ว",
  not_found: "ไม่พบข้อมูล",
  out_of_scope: "นอกเรื่อง",
  no_speech: "ไม่มีคำพูด",
  transcription_failed: "ถอดเสียงไม่ได้",
};

export default function SessionSummaryPage() {
  const params = useParams<{ token: string }>();
  const [session, setSession] = useState<TrainingSession | null | "loading">("loading");
  const [summary, setSummary] = useState<SessionSummary | null>(null);
  const [liveQuestions, setLiveQuestions] = useState<SessionQuestion[]>([]);

  useEffect(() => {
    void api
      .getSessionSummary(params.token)
      .then(async ({ session: found, summary: foundSummary }) => {
        setSession(found);
        setSummary(foundSummary);
        if (!foundSummary) {
          const { questions } = await api.listSessionQuestions(found.id);
          setLiveQuestions(questions);
        }
      })
      .catch(() => setSession(null));
  }, [params.token]);

  if (session === "loading") {
    return (
      <main className="p-6">
        <LoadingBlock label="กำลังโหลดข้อมูลสรุปผลการสอน..." />
      </main>
    );
  }
  if (!session) {
    return <main className="p-6 text-room-muted">ไม่พบ Session นี้ค่ะ</main>;
  }

  const status = getSessionStatus(session);
  const questions = summary?.questions ?? liveQuestions;
  const unresolvedItems =
    summary?.unansweredPoints ?? questions.filter((q) => q.answerStatus === "not_found").map((q) => q.transcript || q.answer || "");

  return (
    <main className="mx-auto max-w-2xl space-y-6 p-6">
      <div>
        <Link href="/admin" className="text-xs text-room-muted hover:text-room-text">
          ← กลับหน้า Admin
        </Link>
        <h1 className="mt-1 text-xl font-semibold text-room-text">สรุปผลการสอน</h1>
        <p className="text-sm text-room-muted">
          {session.teacherName || "ไม่ระบุชื่อคุณครู"} · {session.schoolName || "ไม่ระบุโรงเรียน"}
        </p>
      </div>

      <Card className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <p className="text-room-muted">สถานะ</p>
          <Badge tone={status === "IN_PROGRESS" ? "success" : status === "EXPIRED" ? "danger" : "neutral"}>
            {sessionStatusLabels[status]}
          </Badge>
        </div>
        <div>
          <p className="text-room-muted">สอนครบทุก Slide หรือไม่</p>
          <p className="text-room-text">{summary ? (summary.completedAllSlides ? "ครบ" : "ไม่ครบ") : "ยังไม่จบ Session"}</p>
        </div>
        <div>
          <p className="text-room-muted">Slide ล่าสุด</p>
          <p className="text-room-text">{summary?.lastSlideObjectId ?? session.lastSlideObjectId ?? "-"}</p>
        </div>
        <div>
          <p className="text-room-muted">เวลาเริ่ม - จบ</p>
          <p className="text-room-text">
            {formatDateTimeTh(session.startedAt)} - {formatDateTimeTh(session.endedAt)}
          </p>
        </div>
      </Card>

      <Card className="space-y-2">
        <h2 className="text-sm font-semibold text-room-text">คำถามที่ถามระหว่างการสอน</h2>
        {questions.length === 0 ? (
          <p className="text-sm text-room-muted">ไม่มีคำถามในเซสชันนี้</p>
        ) : (
          <ul className="space-y-2">
            {questions.map((q, i) => (
              <li key={q.id ?? i} className="rounded-lg border border-room-border bg-room-panelAlt p-3 text-sm">
                <p className="text-room-text">
                  {q.transcript || "(ไม่มีคำถอดเสียง)"} <Badge tone="info">{answerStatusLabel[q.answerStatus]}</Badge>
                </p>
                {q.answer && <p className="mt-1 text-room-muted">คำตอบ: {q.answer}</p>}
              </li>
            ))}
          </ul>
        )}
      </Card>

      <Card className="space-y-2">
        <h2 className="text-sm font-semibold text-room-text">จุดที่ตอบไม่ได้ (รอทีม CS ตรวจสอบ)</h2>
        {unresolvedItems.length === 0 ? (
          <p className="text-sm text-room-muted">ไม่มี</p>
        ) : (
          <ul className="list-inside list-disc text-sm text-room-text">
            {unresolvedItems.map((item, i) => (
              <li key={i}>{item}</li>
            ))}
          </ul>
        )}
      </Card>
    </main>
  );
}

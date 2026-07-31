"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { reportRepository, sessionRepository } from "@/providers/data";
import { getSessionStatus, sessionStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import type { SessionSummary, TrainingSession } from "@/types/domain";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";

const scopeLabels = {
  IN_LESSON: "อยู่ในบทเรียน",
  SYSTEM_BASIC: "ระบบพื้นฐาน",
  OUT_OF_SCOPE: "นอกเรื่อง",
  UNKNOWN: "ไม่พบข้อมูล",
} as const;

export default function SessionSummaryPage() {
  const params = useParams<{ id: string }>();
  const [session, setSession] = useState<TrainingSession | null | "loading">("loading");
  const [summary, setSummary] = useState<SessionSummary | null>(null);

  useEffect(() => {
    void sessionRepository.getById(params.id).then(setSession);
    void reportRepository.getBySessionId(params.id).then(setSummary);
  }, [params.id]);

  if (session === "loading") {
    return <main className="p-6 text-room-muted">กำลังโหลดข้อมูล...</main>;
  }
  if (!session) {
    return <main className="p-6 text-room-muted">ไม่พบ Session นี้ค่ะ</main>;
  }

  const status = getSessionStatus(session);
  const lastStepTitle =
    summary?.lastStepTitle ?? session.lessonSnapshot.steps[session.lastStepIndex]?.title ?? "-";

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
          <p className="text-room-muted">สอนครบทุกขั้นตอนหรือไม่</p>
          <p className="text-room-text">
            {summary ? (summary.completedAllSteps ? "ครบ" : "ไม่ครบ") : "ยังไม่จบ Session"}
          </p>
        </div>
        <div>
          <p className="text-room-muted">ขั้นตอนล่าสุด</p>
          <p className="text-room-text">{lastStepTitle}</p>
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
        {!summary || summary.questions.length === 0 ? (
          <p className="text-sm text-room-muted">ไม่มีคำถามในเซสชันนี้</p>
        ) : (
          <ul className="space-y-2">
            {summary.questions.map((q, i) => (
              <li key={i} className="rounded-lg border border-room-border bg-room-panelAlt p-3 text-sm">
                <p className="text-room-text">
                  {q.question} <Badge tone="info">{scopeLabels[q.scope]}</Badge>
                </p>
                {q.answer && <p className="mt-1 text-room-muted">คำตอบ: {q.answer}</p>}
              </li>
            ))}
          </ul>
        )}
      </Card>

      <Card className="space-y-2">
        <h2 className="text-sm font-semibold text-room-text">จุดที่ขอให้อธิบายใหม่ / ทบทวน</h2>
        {!summary || summary.repeatedPoints.length === 0 ? (
          <p className="text-sm text-room-muted">ไม่มี</p>
        ) : (
          <ul className="list-inside list-disc text-sm text-room-text">
            {summary.repeatedPoints.map((point, i) => (
              <li key={i}>{point}</li>
            ))}
          </ul>
        )}
      </Card>

      <Card className="space-y-2">
        <h2 className="text-sm font-semibold text-room-text">คำถามที่ตอบไม่ได้ (รอทีม CS ตรวจสอบ)</h2>
        {!summary || summary.unresolvedItems.length === 0 ? (
          <p className="text-sm text-room-muted">ไม่มี</p>
        ) : (
          <ul className="list-inside list-disc text-sm text-room-text">
            {summary.unresolvedItems.map((item, i) => (
              <li key={i}>{item}</li>
            ))}
          </ul>
        )}
      </Card>
    </main>
  );
}

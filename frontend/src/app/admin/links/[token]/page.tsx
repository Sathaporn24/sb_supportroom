"use client";

import { useEffect, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { learningSessionStatusLabel, linkStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import type { LearningSession, TrainingLink } from "@/types/domain";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";
import { LoadingBlock } from "@/components/ui/LoadingBlock";

/**
 * Everyone who has opened one link. This page used to be a single session's summary - after the
 * link/session split there is no longer "the" session behind a token, so the summary moved to
 * /admin/learning-sessions/[id] and this became the list that leads there.
 */
export default function TrainingLinkDetailPage() {
  const params = useParams<{ token: string }>();
  const [link, setLink] = useState<TrainingLink | null | "loading">("loading");
  const [lessonTitle, setLessonTitle] = useState("");
  const [sessions, setSessions] = useState<LearningSession[]>([]);

  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const { link: found, lessonTitle: title } = await api.getAdminTrainingLinkByToken(params.token);
        if (!active) return;
        setLink(found);
        setLessonTitle(title);
        const { learningSessions } = await api.listLearningSessionsForLink(found.id);
        if (active) setSessions(learningSessions);
      } catch {
        if (active) setLink(null);
      }
    })();
    return () => {
      active = false;
    };
  }, [params.token]);

  if (link === "loading") {
    return (
      <main className="p-6">
        <LoadingBlock label="กำลังโหลดข้อมูลลิงก์..." />
      </main>
    );
  }
  if (!link) {
    return <main className="p-6 text-room-muted">ไม่พบลิงก์นี้ค่ะ</main>;
  }

  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <div>
        <AdminLink href="/admin" className="text-xs text-room-muted hover:text-room-text">
          ← กลับหน้า Admin
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold text-room-text">{lessonTitle}</h1>
        <p className="text-sm text-room-muted">{link.recipientOrgName || "ไม่ระบุหน่วยงาน"}</p>
      </div>

      <Card className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <p className="text-room-muted">สถานะลิงก์</p>
          <Badge tone={link.status === "ACTIVE" ? "success" : "danger"}>{linkStatusLabels[link.status]}</Badge>
        </div>
        <div>
          <p className="text-room-muted">หมดอายุ</p>
          <p className="text-room-text">{formatDateTimeTh(link.expiresAt)}</p>
        </div>
        <div>
          <p className="text-room-muted">สร้างเมื่อ</p>
          <p className="text-room-text">{formatDateTimeTh(link.createdAt)}</p>
        </div>
        <div>
          <p className="text-room-muted">ผู้เข้าเรียน</p>
          <p className="text-room-text">{link.learningSessionCount} คน</p>
        </div>
      </Card>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-room-muted">ผู้เข้าเรียน</h2>
        {sessions.length === 0 ? (
          <p className="rounded-xl border border-dashed border-room-border p-6 text-center text-sm text-room-muted">
            ยังไม่มีใครเปิดลิงก์นี้
          </p>
        ) : (
          <div className="overflow-x-auto rounded-xl border border-room-border">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="bg-room-panelAlt text-xs uppercase tracking-wide text-room-muted">
                <tr>
                  <th className="px-4 py-3">ชื่อ</th>
                  <th className="px-4 py-3">เริ่มเรียน</th>
                  <th className="px-4 py-3">ความคืบหน้า</th>
                  <th className="px-4 py-3">คำถาม</th>
                  <th className="px-4 py-3">สถานะ</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {sessions.map((session) => (
                  <tr key={session.id} className="border-t border-room-border">
                    <td className="px-4 py-3 text-room-text">{session.recipientName}</td>
                    <td className="px-4 py-3 text-room-text">{formatDateTimeTh(session.startedAt)}</td>
                    <td className="px-4 py-3 text-room-text">สไลด์ที่ {session.lastSlideIndex + 1}</td>
                    <td className="px-4 py-3 text-room-text">{session.questionCount}</td>
                    <td className="px-4 py-3">
                      <Badge
                        tone={
                          session.isStalled ? "danger" : session.status === "IN_PROGRESS" ? "success" : "info"
                        }
                      >
                        {learningSessionStatusLabel(session)}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <AdminLink
                        href={`/admin/learning-sessions/${session.id}`}
                        className="rounded-md border border-room-border bg-room-panelAlt px-2.5 py-1.5 text-xs text-room-text hover:border-room-accent/60"
                      >
                        ดูสรุป
                      </AdminLink>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}

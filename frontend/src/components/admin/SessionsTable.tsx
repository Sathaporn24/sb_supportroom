import Link from "next/link";
import type { TrainingSession } from "@/types/domain";
import { getSessionStatus, sessionStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/Badge";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";

const statusTone = {
  NOT_STARTED: "neutral",
  IN_PROGRESS: "success",
  ENDED: "info",
  EXPIRED: "danger",
} as const;

export function SessionsTable({ sessions, origin }: { sessions: TrainingSession[]; origin: string }) {
  if (sessions.length === 0) {
    return (
      <p className="rounded-xl border border-dashed border-room-border p-6 text-center text-sm text-room-muted">
        ยังไม่มี Mock Session ลองสร้างลิงก์การสอนใหม่ได้เลยค่ะ
      </p>
    );
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-room-border">
      <table className="w-full min-w-[720px] text-left text-sm">
        <thead className="bg-room-panelAlt text-xs uppercase tracking-wide text-room-muted">
          <tr>
            <th className="px-4 py-3">วันที่สร้าง</th>
            <th className="px-4 py-3">ผู้รับลิงก์</th>
            <th className="px-4 py-3">องค์กร</th>
            <th className="px-4 py-3">หมดอายุ</th>
            <th className="px-4 py-3">สถานะ</th>
            <th className="px-4 py-3">การจัดการ</th>
          </tr>
        </thead>
        <tbody>
          {sessions.map((session) => {
            const status = getSessionStatus(session);
            return (
              <tr key={session.id} className="border-t border-room-border">
                <td className="px-4 py-3 text-room-text">{formatDateTimeTh(session.createdAt)}</td>
                <td className="px-4 py-3 text-room-text">{session.recipientName || "ไม่ระบุ"}</td>
                <td className="px-4 py-3 text-room-text">{session.recipientOrgName || "ไม่ระบุ"}</td>
                <td className="px-4 py-3 text-room-text">{formatDateTimeTh(session.expiresAt)}</td>
                <td className="px-4 py-3">
                  <Badge tone={statusTone[status]}>{sessionStatusLabels[status]}</Badge>
                </td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <CopyLinkButton url={`${origin}/join/${session.token}`} />
                    <Link
                      href={`/admin/sessions/${session.token}`}
                      className="rounded-md border border-room-border bg-room-panelAlt px-2.5 py-1.5 text-xs text-room-text hover:border-room-accent/60"
                    >
                      ดูสรุป
                    </Link>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

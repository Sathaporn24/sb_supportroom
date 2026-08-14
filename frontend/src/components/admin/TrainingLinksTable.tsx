import { AdminLink } from "@/components/admin/AdminLink";
import type { TrainingLink } from "@/types/domain";
import { linkStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/Badge";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";

const statusTone = {
  ACTIVE: "success",
  EXPIRED: "danger",
} as const;

export function TrainingLinksTable({ links, origin }: { links: TrainingLink[]; origin: string }) {
  if (links.length === 0) {
    return (
      <p className="rounded-xl border border-dashed border-room-border p-6 text-center text-sm text-room-muted">
        ยังไม่มีลิงก์ ลองสร้างลิงก์การเรียนใหม่ได้เลยค่ะ
      </p>
    );
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-room-border">
      <table className="w-full min-w-[720px] text-left text-sm">
        <thead className="bg-room-panelAlt text-xs uppercase tracking-wide text-room-muted">
          <tr>
            <th className="px-4 py-3">วันที่สร้าง</th>
            <th className="px-4 py-3">หน่วยงาน</th>
            {/* Replaces the old "ผู้รับลิงก์" column: CS no longer types a name, because one link
                is opened by many people who each type their own. */}
            <th className="px-4 py-3">ผู้เข้าเรียน</th>
            <th className="px-4 py-3">หมดอายุ</th>
            <th className="px-4 py-3">สถานะ</th>
            <th className="px-4 py-3">การจัดการ</th>
          </tr>
        </thead>
        <tbody>
          {links.map((link) => (
            <tr key={link.id} className="border-t border-room-border">
              <td className="px-4 py-3 text-room-text">{formatDateTimeTh(link.createdAt)}</td>
              <td className="px-4 py-3 text-room-text">{link.recipientOrgName || "ไม่ระบุ"}</td>
              <td className="px-4 py-3 text-room-text">{link.learningSessionCount} คน</td>
              <td className="px-4 py-3 text-room-text">{formatDateTimeTh(link.expiresAt)}</td>
              <td className="px-4 py-3">
                <Badge tone={statusTone[link.status]}>{linkStatusLabels[link.status]}</Badge>
              </td>
              <td className="px-4 py-3">
                <div className="flex flex-wrap items-center gap-2">
                  <CopyLinkButton url={`${origin}/join/${link.token}`} />
                  <AdminLink
                    href={`/admin/links/${link.token}`}
                    className="rounded-md border border-room-border bg-room-panelAlt px-2.5 py-1.5 text-xs text-room-text hover:border-room-accent/60"
                  >
                    ดูผู้เข้าเรียน
                  </AdminLink>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

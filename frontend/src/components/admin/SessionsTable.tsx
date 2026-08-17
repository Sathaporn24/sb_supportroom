import Link from "next/link";
import type { TrainingSession } from "@/types/domain";
import { getSessionStatus, sessionStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";

const statusVariant = {
  NOT_STARTED: "secondary",
  IN_PROGRESS: "default",
  ENDED: "outline",
  EXPIRED: "destructive",
} as const;

export function SessionsTable({ sessions, origin }: { sessions: TrainingSession[]; origin: string }) {
  if (sessions.length === 0) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>ยังไม่มี Session</EmptyTitle>
          <EmptyDescription>ลองสร้างลิงก์การสอนใหม่ได้เลยค่ะ</EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <div className="overflow-hidden rounded-xl border">
      <Table className="min-w-[720px]">
        <TableHeader>
          <TableRow>
            <TableHead className="px-4">วันที่สร้าง</TableHead>
            <TableHead className="px-4">ผู้รับลิงก์</TableHead>
            <TableHead className="px-4">องค์กร</TableHead>
            <TableHead className="px-4">หมดอายุ</TableHead>
            <TableHead className="px-4">สถานะ</TableHead>
            <TableHead className="px-4">การจัดการ</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {sessions.map((session) => {
            const status = getSessionStatus(session);
            return (
              <TableRow key={session.id}>
                <TableCell className="px-4 py-3">{formatDateTimeTh(session.createdAt)}</TableCell>
                <TableCell className="px-4 py-3">{session.recipientName || "ไม่ระบุ"}</TableCell>
                <TableCell className="px-4 py-3">{session.recipientOrgName || "ไม่ระบุ"}</TableCell>
                <TableCell className="px-4 py-3">{formatDateTimeTh(session.expiresAt)}</TableCell>
                <TableCell className="px-4 py-3">
                  <Badge variant={statusVariant[status]}>{sessionStatusLabels[status]}</Badge>
                </TableCell>
                <TableCell className="px-4 py-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <CopyLinkButton url={`${origin}/join/${session.token}`} />
                    <Link
                      href={`/admin/sessions/${session.token}`}
                      className={buttonVariants({ variant: "outline", size: "sm" })}
                    >
                      ดูสรุป
                    </Link>
                  </div>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}

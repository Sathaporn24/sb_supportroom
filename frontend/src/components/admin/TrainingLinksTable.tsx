import { AdminLink } from "@/components/admin/AdminLink";
import type { TrainingLink } from "@/types/domain";
import { linkStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";

const statusVariant = {
  ACTIVE: "default",
  EXPIRED: "destructive",
} as const;

export function TrainingLinksTable({ links, origin }: { links: TrainingLink[]; origin: string }) {
  if (links.length === 0) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>ยังไม่มีลิงก์</EmptyTitle>
          <EmptyDescription>ลองสร้างลิงก์การเรียนใหม่ได้เลยค่ะ</EmptyDescription>
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
            <TableHead className="px-4">หน่วยงาน</TableHead>
            {/* Replaces the old "ผู้รับลิงก์" column: CS no longer types a name, because one link
                is opened by many people who each type their own. */}
            <TableHead className="px-4">ผู้เข้าเรียน</TableHead>
            <TableHead className="px-4">หมดอายุ</TableHead>
            <TableHead className="px-4">สถานะ</TableHead>
            <TableHead className="px-4">การจัดการ</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {links.map((link) => (
            <TableRow key={link.id}>
              <TableCell className="px-4 py-3">{formatDateTimeTh(link.createdAt)}</TableCell>
              <TableCell className="px-4 py-3">{link.recipientOrgName || "ไม่ระบุ"}</TableCell>
              <TableCell className="px-4 py-3">{link.learningSessionCount} คน</TableCell>
              <TableCell className="px-4 py-3">{formatDateTimeTh(link.expiresAt)}</TableCell>
              <TableCell className="px-4 py-3">
                <Badge variant={statusVariant[link.status]}>{linkStatusLabels[link.status]}</Badge>
              </TableCell>
              <TableCell className="px-4 py-3">
                <div className="flex flex-wrap items-center gap-2">
                  <CopyLinkButton url={`${origin}/join/${link.token}`} />
                  <AdminLink
                    href={`/admin/links/${link.token}`}
                    className={buttonVariants({ variant: "outline", size: "sm" })}
                  >
                    ดูผู้เข้าเรียน
                  </AdminLink>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

"use client";

import { useMemo, useState } from "react";
import { SearchIcon } from "lucide-react";
import { AdminLink } from "@/components/admin/AdminLink";
import type { TrainingLink } from "@/types/domain";
import { isLinkExpiringSoon, linkStatusLabels, LINK_EXPIRING_SOON_LABEL } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Pagination, PaginationContent, PaginationItem } from "@/components/ui/pagination";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";

const statusVariant = {
  ACTIVE: "default",
  EXPIRED: "destructive",
} as const;

const LINKS_PER_PAGE = 10;

function matchesSearch(link: TrainingLink, query: string): boolean {
  const haystack = `${link.lessonSlug} ${link.recipientOrgName ?? ""}`.toLowerCase();
  return haystack.includes(query.toLowerCase());
}

export function TrainingLinksTable({ links, origin }: { links: TrainingLink[]; origin: string }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const filtered = useMemo(() => {
    const query = search.trim();
    return query ? links.filter((link) => matchesSearch(link, query)) : links;
  }, [links, search]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / LINKS_PER_PAGE));
  const currentPage = Math.min(page, totalPages);
  const pageLinks = filtered.slice((currentPage - 1) * LINKS_PER_PAGE, currentPage * LINKS_PER_PAGE);

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

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
    <div className="flex flex-col gap-3">
      <div className="relative max-w-xs">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={search}
          onChange={(event) => handleSearchChange(event.target.value)}
          placeholder="ค้นหาบทเรียนหรือหน่วยงาน..."
          className="pl-8"
          data-testid="training-links-table-search-input"
        />
      </div>

      {pageLinks.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ไม่พบลิงก์ที่ตรงกับคำค้นหา</EmptyTitle>
            <EmptyDescription>ลองค้นหาด้วยคำอื่น</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[760px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">วันที่สร้าง</TableHead>
                <TableHead className="px-4">หน่วยงาน</TableHead>
                {/* Replaces the old "ผู้รับลิงก์" column: CS no longer types a name, because one link
                    is opened by many people who each type their own. */}
                <TableHead className="px-4">ภาพรวมการเรียน</TableHead>
                <TableHead className="px-4">หมดอายุ</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4">การจัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageLinks.map((link) => (
                <TableRow key={link.id} data-testid={`training-link-row-${link.token}`}>
                  <TableCell className="px-4 py-3">{formatDateTimeTh(link.createdAt)}</TableCell>
                  <TableCell className="px-4 py-3">{link.recipientOrgName || "ไม่ระบุ"}</TableCell>
                  <TableCell className="px-4 py-3">
                    <div className="flex min-w-32 flex-col gap-1 text-xs">
                      <span className="font-medium text-foreground">ผู้เรียน {link.learnerCount} คน</span>
                      <span className="text-muted-foreground">กำลังเรียน {link.inProgressCount} รอบ</span>
                      <span className="text-muted-foreground">จบแล้ว {link.endedCount} รอบ</span>
                    </div>
                  </TableCell>
                  <TableCell className="px-4 py-3">{formatDateTimeTh(link.expiresAt)}</TableCell>
                  <TableCell className="px-4 py-3">
                    <div className="flex flex-wrap gap-1.5">
                      <Badge variant={statusVariant[link.status]}>{linkStatusLabels[link.status]}</Badge>
                      {link.status === "ACTIVE" && isLinkExpiringSoon(link) && (
                        <Badge variant="outline">{LINK_EXPIRING_SOON_LABEL}</Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="px-4 py-3">
                    <div className="flex flex-wrap items-center gap-2">
                      <CopyLinkButton
                        url={`${origin}/join/${link.token}`}
                        testId={`training-link-row-${link.token}-copy-button`}
                      />
                      <AdminLink
                        href={`/admin/links/${link.token}`}
                        className={buttonVariants({ variant: "outline", size: "sm" })}
                        data-testid={`training-link-row-${link.token}-view-learners-link`}
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
      )}

      {totalPages > 1 && (
        <Pagination className="justify-between">
          <span className="text-xs text-muted-foreground">
            หน้า {currentPage} จาก {totalPages}
          </span>
          <PaginationContent>
            <PaginationItem>
              <Button
                variant="ghost"
                size="sm"
                disabled={currentPage <= 1}
                onClick={() => setPage((current) => Math.max(1, current - 1))}
                data-testid="training-links-table-prev-page-button"
              >
                ก่อนหน้า
              </Button>
            </PaginationItem>
            <PaginationItem>
              <Button
                variant="ghost"
                size="sm"
                disabled={currentPage >= totalPages}
                onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                data-testid="training-links-table-next-page-button"
              >
                ถัดไป
              </Button>
            </PaginationItem>
          </PaginationContent>
        </Pagination>
      )}
    </div>
  );
}

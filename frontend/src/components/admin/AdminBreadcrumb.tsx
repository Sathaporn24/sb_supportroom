"use client";

import { usePathname } from "next/navigation";
import { ChevronRightIcon } from "lucide-react";
import { AdminLink } from "@/components/admin/AdminLink";

/** Matched most-specific-first against the pathname; the first match wins. Keep this list in
 * sync with each page's own <h1> - it exists only because Figma's navbar shows "หน้าแรก > current
 * page" and nothing in the app tracked a page title before this. */
const ROUTE_LABELS: { test: RegExp; label: string }[] = [
  { test: /^\/admin\/lessons\/new/, label: "สร้างบทเรียนใหม่" },
  { test: /^\/admin\/lessons\/[^/]+\/narrations/, label: "บทพูดต่อหน้า" },
  { test: /^\/admin\/lessons\/[^/]+/, label: "แก้ไขบทเรียน" },
  { test: /^\/admin\/lessons/, label: "บทเรียน" },
  { test: /^\/admin\/links\/new/, label: "สร้างลิงก์การเรียน" },
  { test: /^\/admin\/links\/[^/]+/, label: "รายละเอียดลิงก์" },
  { test: /^\/admin\/documents\/[^/]+\/chunks/, label: "ข้อความที่แปลงได้" },
  { test: /^\/admin\/documents/, label: "คลังเอกสาร" },
  { test: /^\/admin\/qna-queue/, label: "คำถามรอคำตอบ" },
  { test: /^\/admin\/qna-conflicts/, label: "Q&A ขัดกับเอกสาร" },
  { test: /^\/admin\/users/, label: "จัดการผู้ใช้" },
  { test: /^\/admin\/companies\/new/, label: "สร้างบริษัทใหม่" },
  { test: /^\/admin\/settings/, label: "ตั้งค่าบริษัท" },
  { test: /^\/admin\/change-password/, label: "เปลี่ยนรหัสผ่าน" },
  { test: /^\/admin\/learning-sessions\/[^/]+/, label: "สรุปการเรียน" },
];

export function AdminBreadcrumb() {
  const pathname = usePathname();
  const current = ROUTE_LABELS.find(({ test }) => test.test(pathname));

  return (
    <nav className="flex items-center gap-1.5 text-sm" aria-label="breadcrumb">
      <AdminLink
        href="/admin"
        className={current ? "text-muted-foreground hover:text-foreground" : "text-foreground"}
        data-testid="admin-breadcrumb-home-link"
      >
        หน้าแรก
      </AdminLink>
      {current && (
        <>
          <ChevronRightIcon className="size-3.5 shrink-0 text-muted-foreground" />
          <span className="text-foreground">{current.label}</span>
        </>
      )}
    </nav>
  );
}

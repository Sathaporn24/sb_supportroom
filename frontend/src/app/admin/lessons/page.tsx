"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import * as api from "@/lib/api-client";
import type { LessonConfig } from "@/types/domain";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { LoadingBlock } from "@/components/shared/LoadingBlock";

export default function LessonsListPage() {
  const [lessons, setLessons] = useState<LessonConfig[] | null>(null);

  useEffect(() => {
    void api.listLessons().then(({ lessons: list }) => setLessons(list));
  }, []);

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <Link href="/admin" className="text-xs text-muted-foreground hover:text-foreground">
          ← กลับหน้า Admin
        </Link>
        <h1 className="mt-1 text-xl font-semibold">บทเรียน (Google Slides)</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          แต่ละบทเรียนดึงเนื้อหาจาก Google Slides โดยตรง — 1 Slide = 1 ช่วงการสอน และ Speaker Notes คือบทพูด
        </p>
      </div>

      {!lessons ? (
        <LoadingBlock label="กำลังโหลดรายการบทเรียน..." />
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[520px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">ชื่อบทเรียน</TableHead>
                <TableHead className="px-4">Slug</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4">จัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {lessons.map((lesson) => (
                <TableRow key={lesson.id}>
                  <TableCell className="px-4 py-3 font-medium">{lesson.title}</TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{lesson.slug}</TableCell>
                  <TableCell className="px-4 py-3">
                    <Badge variant={lesson.isActive ? "default" : "secondary"}>
                      {lesson.isActive ? "พร้อมใช้งาน" : "ปิดใช้งาน"}
                    </Badge>
                  </TableCell>
                  <TableCell className="px-4 py-3">
                    <Link
                      href={`/admin/lessons/${encodeURIComponent(lesson.slug)}`}
                      className={buttonVariants({ variant: "outline", size: "sm" })}
                    >
                      แก้ไข
                    </Link>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </main>
  );
}

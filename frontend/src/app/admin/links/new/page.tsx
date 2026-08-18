"use client";

import { useEffect, useMemo, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import * as api from "@/lib/api-client";
import type { LessonConfig } from "@/types/domain";
import { CreateTrainingLinkModal } from "@/components/admin/CreateTrainingLinkModal";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { LoadingBlock } from "@/components/shared/LoadingBlock";

export default function NewTrainingLinkPage() {
  // null = still loading, distinct from "loaded but empty" - a bare [] initial value
  // made this page flash "ไม่พบสื่อการสอนที่ค้นหา" every time while the real fetch was
  // still in flight.
  const [lessons, setLessons] = useState<LessonConfig[] | null>(null);
  const [query, setQuery] = useState("");
  const [selectedLesson, setSelectedLesson] = useState<LessonConfig | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    void api.listLessons().then(({ lessons: list }) => setLessons(list));
  }, []);

  const filtered = useMemo(() => {
    if (!lessons) return [];
    const q = query.trim().toLowerCase();
    if (!q) return lessons;
    return lessons.filter((lesson) => lesson.title.toLowerCase().includes(q));
  }, [lessons, query]);

  function openModalFor(lesson: LessonConfig) {
    setSelectedLesson(lesson);
    setModalOpen(true);
  }

  return (
    <main className="mx-auto flex max-w-4xl flex-col gap-6 p-6">
      <div>
        <AdminLink href="/admin" className="text-xs text-muted-foreground hover:text-foreground">
          ← กลับหน้า Admin
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold">สร้างลิงก์การเรียน</h1>
        <p className="mt-1 text-sm text-muted-foreground">เลือกสื่อการสอนที่ต้องการสร้างลิงก์ห้องเรียนให้ผู้ใช้</p>
      </div>

      <Input
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="ค้นหาสื่อการสอน..."
        aria-label="ค้นหาสื่อการสอน"
        className="h-10"
      />

      {lessons === null ? (
        <LoadingBlock label="กำลังโหลดรายการสื่อการสอน..." />
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[520px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">ลำดับ</TableHead>
                <TableHead className="px-4">สื่อการสอน</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4">จัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((lesson, index) => (
                <TableRow key={lesson.id}>
                  <TableCell className="px-4 py-3 text-muted-foreground">{index + 1}</TableCell>
                  <TableCell className="px-4 py-3 font-medium">{lesson.title}</TableCell>
                  <TableCell className="px-4 py-3">
                    <Badge variant={lesson.isActive ? "default" : "secondary"}>
                      {lesson.isActive ? "พร้อมใช้งาน" : "เร็วๆ นี้"}
                    </Badge>
                  </TableCell>
                  <TableCell className="px-4 py-3">
                    <Button
                      onClick={() => openModalFor(lesson)}
                      disabled={!lesson.isActive}
                      variant={lesson.isActive ? "default" : "ghost"}
                    >
                      สร้างลิงก์การเรียน
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {filtered.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} className="px-4 py-6 text-center text-muted-foreground">
                    ไม่พบสื่อการสอนที่ค้นหา
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      )}

      <CreateTrainingLinkModal open={modalOpen} onClose={() => setModalOpen(false)} lesson={selectedLesson} />
    </main>
  );
}

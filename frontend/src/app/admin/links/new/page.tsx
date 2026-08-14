"use client";

import { useEffect, useMemo, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import * as api from "@/lib/api-client";
import type { LessonConfig } from "@/types/domain";
import { CreateTrainingLinkModal } from "@/components/admin/CreateTrainingLinkModal";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { LoadingBlock } from "@/components/ui/LoadingBlock";

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
    <main className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <AdminLink href="/admin" className="text-xs text-room-muted hover:text-room-text">
          ← กลับหน้า Admin
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold text-room-text">สร้างลิงก์การเรียน</h1>
        <p className="mt-1 text-sm text-room-muted">เลือกสื่อการสอนที่ต้องการสร้างลิงก์ห้องเรียนให้ผู้ใช้</p>
      </div>

      <input
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="ค้นหาสื่อการสอน..."
        aria-label="ค้นหาสื่อการสอน"
        className="w-full rounded-lg border border-room-border bg-room-bg px-4 py-2.5 text-sm text-room-text outline-none focus:border-room-accent"
      />

      {lessons === null ? (
        <LoadingBlock label="กำลังโหลดรายการสื่อการสอน..." />
      ) : (
        <div className="overflow-x-auto rounded-xl border border-room-border">
          <table className="w-full min-w-[520px] text-left text-sm">
            <thead className="bg-room-panelAlt text-xs uppercase tracking-wide text-room-muted">
              <tr>
                <th className="px-4 py-3">ลำดับ</th>
                <th className="px-4 py-3">สื่อการสอน</th>
                <th className="px-4 py-3">สถานะ</th>
                <th className="px-4 py-3">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((lesson, index) => (
                <tr key={lesson.id} className="border-t border-room-border">
                  <td className="px-4 py-3 text-room-muted">{index + 1}</td>
                  <td className="px-4 py-3 font-medium text-room-text">{lesson.title}</td>
                  <td className="px-4 py-3">
                    {lesson.isActive ? (
                      <Badge tone="success">
                        <span className="whitespace-nowrap">พร้อมใช้งาน</span>
                      </Badge>
                    ) : (
                      <Badge>
                        <span className="whitespace-nowrap">เร็วๆ นี้</span>
                      </Badge>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <Button
                      onClick={() => openModalFor(lesson)}
                      disabled={!lesson.isActive}
                      variant={lesson.isActive ? "primary" : "ghost"}
                      className="whitespace-nowrap"
                    >
                      สร้างลิงก์การเรียน
                    </Button>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-room-muted">
                    ไม่พบสื่อการสอนที่ค้นหา
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <CreateTrainingLinkModal open={modalOpen} onClose={() => setModalOpen(false)} lesson={selectedLesson} />
    </main>
  );
}

"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import * as api from "@/lib/api-client";
import type { LessonConfig } from "@/types/domain";
import { Badge } from "@/components/ui/Badge";

export default function LessonsListPage() {
  const [lessons, setLessons] = useState<LessonConfig[] | null>(null);

  useEffect(() => {
    void api.listLessons().then(({ lessons: list }) => setLessons(list));
  }, []);

  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <div>
        <Link href="/admin" className="text-xs text-room-muted hover:text-room-text">
          ← กลับหน้า Admin
        </Link>
        <h1 className="mt-1 text-xl font-semibold text-room-text">บทเรียน (Google Slides)</h1>
        <p className="mt-1 text-sm text-room-muted">
          แต่ละบทเรียนดึงเนื้อหาจาก Google Slides โดยตรง — 1 Slide = 1 ช่วงการสอน และ Speaker Notes คือบทพูด
        </p>
      </div>

      {!lessons ? (
        <p className="text-sm text-room-muted">กำลังโหลด...</p>
      ) : (
        <div className="overflow-x-auto rounded-xl border border-room-border">
          <table className="w-full min-w-[520px] text-left text-sm">
            <thead className="bg-room-panelAlt text-xs uppercase tracking-wide text-room-muted">
              <tr>
                <th className="px-4 py-3">ชื่อบทเรียน</th>
                <th className="px-4 py-3">Slug</th>
                <th className="px-4 py-3">สถานะ</th>
                <th className="px-4 py-3">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {lessons.map((lesson) => (
                <tr key={lesson.id} className="border-t border-room-border">
                  <td className="px-4 py-3 font-medium text-room-text">{lesson.title}</td>
                  <td className="px-4 py-3 text-room-muted">{lesson.slug}</td>
                  <td className="px-4 py-3">
                    <Badge tone={lesson.isActive ? "success" : "neutral"}>
                      <span className="whitespace-nowrap">{lesson.isActive ? "พร้อมใช้งาน" : "ปิดใช้งาน"}</span>
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <Link
                      href={`/admin/lessons/${encodeURIComponent(lesson.slug)}`}
                      className="rounded-md border border-room-border bg-room-panelAlt px-2.5 py-1.5 text-xs text-room-text hover:border-room-accent/60"
                    >
                      แก้ไข
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}

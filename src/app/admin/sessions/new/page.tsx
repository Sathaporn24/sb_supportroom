"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { lessonRepository } from "@/providers/data";
import type { Lesson } from "@/types/domain";
import { teachingTopicsSeed } from "@/mocks/teaching-topics.mock";
import { CreateSessionModal } from "@/components/admin/CreateSessionModal";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";

export default function NewSessionPage() {
  const [lesson, setLesson] = useState<Lesson | null>(null);
  const [query, setQuery] = useState("");
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    void lessonRepository.getLoginLesson().then(setLesson);
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) {
      return teachingTopicsSeed;
    }
    return teachingTopicsSeed.filter((topic) => topic.title.toLowerCase().includes(q));
  }, [query]);

  return (
    <main className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <Link href="/admin" className="text-xs text-room-muted hover:text-room-text">
          ← กลับหน้า Admin
        </Link>
        <h1 className="mt-1 text-xl font-semibold text-room-text">สร้างลิงก์การสอน</h1>
        <p className="mt-1 text-sm text-room-muted">เลือกสื่อการสอนที่ต้องการสร้างลิงก์ห้องสอนให้คุณครู</p>
      </div>

      <input
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="ค้นหาสื่อการสอน..."
        aria-label="ค้นหาสื่อการสอน"
        className="w-full rounded-lg border border-room-border bg-room-bg px-4 py-2.5 text-sm text-room-text outline-none focus:border-room-accent"
      />

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
            {filtered.map((topic, index) => (
              <tr key={topic.id} className="border-t border-room-border">
                <td className="px-4 py-3 text-room-muted">{index + 1}</td>
                <td className="px-4 py-3 font-medium text-room-text">{topic.title}</td>
                <td className="px-4 py-3">
                  {topic.available ? (
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
                  {topic.available ? (
                    <Button onClick={() => setModalOpen(true)} className="whitespace-nowrap">
                      สร้างลิงก์การสอน
                    </Button>
                  ) : (
                    <Button variant="ghost" disabled className="whitespace-nowrap">
                      สร้างลิงก์การสอน
                    </Button>
                  )}
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

      <CreateSessionModal open={modalOpen} onClose={() => setModalOpen(false)} lesson={lesson} />
    </main>
  );
}

"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { lessonRepository } from "@/providers/data";
import type { Lesson } from "@/types/domain";
import { StepList } from "@/components/lesson/StepList";
import { FaqList } from "@/components/lesson/FaqList";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";

export default function LessonEditorPage() {
  const [lesson, setLesson] = useState<Lesson | null>(null);
  const [savedAt, setSavedAt] = useState<string | null>(null);

  useEffect(() => {
    void lessonRepository.getLoginLesson().then(setLesson);
  }, []);

  if (!lesson) {
    return <main className="p-6 text-room-muted">กำลังโหลดบทเรียน...</main>;
  }

  async function handleSave() {
    if (!lesson) return;
    const saved = await lessonRepository.saveLoginLesson(lesson);
    setLesson(saved);
    setSavedAt(new Date().toLocaleTimeString("th-TH"));
  }

  async function handleReset() {
    const seeded = await lessonRepository.resetLoginLesson();
    setLesson(seeded);
    setSavedAt(null);
  }

  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <Link href="/admin" className="text-xs text-room-muted hover:text-room-text">
            ← กลับหน้า Admin
          </Link>
          <h1 className="mt-1 text-xl font-semibold text-room-text">แก้ไขบทเรียน Login</h1>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="ghost" onClick={handleReset}>
            คืนค่าเริ่มต้น
          </Button>
          <Button onClick={handleSave}>บันทึก</Button>
        </div>
      </div>

      {savedAt && <p className="text-xs text-room-accent">บันทึกแล้วเมื่อ {savedAt}</p>}

      <Card>
        <label className="block text-sm">
          <span className="mb-1 block text-room-muted">ชื่อบทเรียน</span>
          <input
            value={lesson.title}
            onChange={(e) => setLesson({ ...lesson, title: e.target.value })}
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
          />
        </label>
      </Card>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-room-muted">ขั้นตอนการสอน</h2>
        <StepList steps={lesson.steps} onChange={(steps) => setLesson({ ...lesson, steps })} />
      </section>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-room-muted">คำถามที่พบบ่อย (FAQ)</h2>
        <FaqList faqs={lesson.faqs} onChange={(faqs) => setLesson({ ...lesson, faqs })} />
      </section>

      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={handleReset}>
          คืนค่าเริ่มต้น
        </Button>
        <Button onClick={handleSave}>บันทึก</Button>
      </div>
    </main>
  );
}

"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { lessonRepository, sessionRepository } from "@/providers/data";
import { tutorConfig } from "@/config/tutor-config";
import { addHours } from "@/utils/format";
import type { Lesson, TrainingSession } from "@/types/domain";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";

export default function NewSessionPage() {
  const [lesson, setLesson] = useState<Lesson | null>(null);
  const [teacherName, setTeacherName] = useState("");
  const [schoolName, setSchoolName] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [created, setCreated] = useState<TrainingSession | null>(null);
  const [origin, setOrigin] = useState("");

  useEffect(() => {
    void lessonRepository.getLoginLesson().then(setLesson);
    setOrigin(window.location.origin);
    const defaultExpiry = addHours(new Date().toISOString(), tutorConfig.defaultLinkExpiryHours);
    setExpiresAt(toLocalInputValue(defaultExpiry));
  }, []);

  if (!lesson) {
    return <main className="p-6 text-room-muted">กำลังโหลดบทเรียน...</main>;
  }

  async function handleCreate() {
    const session = await sessionRepository.create({
      teacherName: teacherName || undefined,
      schoolName: schoolName || undefined,
      expiresAt: new Date(expiresAt).toISOString(),
    });
    setCreated(session);
  }

  if (created) {
    const url = `${origin}/join/${created.token}`;
    return (
      <main className="mx-auto max-w-lg space-y-4 p-6">
        <Card className="space-y-4">
          <h1 className="text-lg font-semibold text-room-text">สร้างลิงก์การสอนสำเร็จ</h1>
          <p className="break-all rounded-lg border border-room-border bg-room-panelAlt px-3 py-2 text-sm text-room-text">
            {url}
          </p>
          <div className="flex flex-wrap gap-2">
            <CopyLinkButton url={url} />
            <Link href="/admin">
              <Button variant="ghost">กลับหน้า Admin</Button>
            </Link>
          </div>
          <p className="text-xs text-room-muted">
            หมายเหตุ: ลิงก์นี้ใช้งานได้เฉพาะ Browser Profile เดียวกันในเฟสนี้ ลองเปิดในแท็บใหม่ของเบราว์เซอร์เดียวกันได้เลยค่ะ
          </p>
        </Card>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-lg space-y-6 p-6">
      <div>
        <Link href="/admin" className="text-xs text-room-muted hover:text-room-text">
          ← กลับหน้า Admin
        </Link>
        <h1 className="mt-1 text-xl font-semibold text-room-text">สร้างลิงก์การสอนใหม่</h1>
      </div>

      <Card className="space-y-2">
        <p className="text-xs uppercase tracking-wide text-room-muted">บทเรียน (อ่านอย่างเดียว)</p>
        <p className="text-sm font-medium text-room-text">{lesson.title}</p>
        <p className="text-xs text-room-muted">{lesson.steps.length} ขั้นตอน</p>
      </Card>

      <Card className="space-y-4">
        <label className="block text-sm">
          <span className="mb-1 block text-room-muted">ชื่อคุณครู (ไม่บังคับ)</span>
          <input
            value={teacherName}
            onChange={(e) => setTeacherName(e.target.value)}
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
          />
        </label>
        <label className="block text-sm">
          <span className="mb-1 block text-room-muted">โรงเรียน (ไม่บังคับ)</span>
          <input
            value={schoolName}
            onChange={(e) => setSchoolName(e.target.value)}
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
          />
        </label>
        <label className="block text-sm">
          <span className="mb-1 block text-room-muted">วันหมดอายุลิงก์</span>
          <input
            type="datetime-local"
            value={expiresAt}
            onChange={(e) => setExpiresAt(e.target.value)}
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
          />
        </label>
        <Button className="w-full" onClick={handleCreate}>
          สร้างลิงก์การสอน
        </Button>
      </Card>
    </main>
  );
}

function toLocalInputValue(iso: string): string {
  const date = new Date(iso);
  const offsetMs = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

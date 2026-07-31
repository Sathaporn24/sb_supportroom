"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import * as api from "@/lib/api-client";
import type { TrainingSession } from "@/types/domain";
import { SessionsTable } from "@/components/admin/SessionsTable";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";

export default function AdminPage() {
  const [sessions, setSessions] = useState<TrainingSession[]>([]);
  const [origin, setOrigin] = useState("");
  const [loading, setLoading] = useState(true);

  async function reload() {
    setLoading(true);
    const { sessions: list } = await api.listSessions();
    setSessions(list);
    setLoading(false);
  }

  useEffect(() => {
    setOrigin(window.location.origin);
    void reload();
  }, []);

  async function handleReset() {
    const confirmed = window.confirm("ต้องการรีเซ็ตข้อมูล Demo ทั้งหมดกลับเป็นค่าเริ่มต้นใช่หรือไม่?");
    if (!confirmed) return;
    await api.resetDemoData();
    await reload();
  }

  return (
    <main className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-room-text">SupportRoom AI — Admin</h1>
        <p className="mt-1 text-sm text-room-muted">
          จัดการบทเรียนจาก Google Slides และสร้างลิงก์ห้องสอนการใช้งานระบบสำหรับคุณครู
        </p>
      </div>

      <div className="flex flex-wrap gap-3">
        <Link href="/admin/lessons">
          <Button variant="secondary">จัดการบทเรียน</Button>
        </Link>
        <Link href="/admin/sessions/new">
          <Button>สร้างลิงก์การสอน</Button>
        </Link>
        <Button variant="ghost" onClick={handleReset}>
          Reset Demo Data
        </Button>
      </div>

      <Card className="border-amber-500/30 bg-amber-500/5">
        <p className="text-xs text-amber-700">
          หมายเหตุ: ในโหมด Mock ข้อมูลเก็บแบบ In-memory ฝั่งเซิร์ฟเวอร์ (รีเซ็ตเมื่อรีสตาร์ทเซิร์ฟเวอร์) ลิงก์ใช้งานได้ข้ามเบราว์เซอร์/อุปกรณ์ได้
          ตราบใดที่ยังเรียกไปที่เซิร์ฟเวอร์เดียวกัน — เมื่อสลับไปใช้ Supabase ข้อมูลจะถาวรและใช้งานข้ามอุปกรณ์ได้เต็มรูปแบบ
        </p>
      </Card>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-room-muted">รายการ Sessions</h2>
        {loading ? (
          <p className="text-sm text-room-muted">กำลังโหลด...</p>
        ) : (
          <SessionsTable sessions={sessions} origin={origin} />
        )}
      </section>
    </main>
  );
}

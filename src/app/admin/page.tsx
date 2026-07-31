"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { sessionRepository, resetAllDemoData } from "@/providers/data";
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
    const list = await sessionRepository.list();
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
    await resetAllDemoData();
    await reload();
  }

  return (
    <main className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-room-text">SupportRoom AI — Admin</h1>
        <p className="mt-1 text-sm text-room-muted">
          จัดการบทเรียนและสร้างลิงก์ห้องสอนการใช้งานระบบสำหรับคุณครู
        </p>
      </div>

      <div className="flex flex-wrap gap-3">
        <Link href="/admin/lesson">
          <Button variant="secondary">จัดการบทเรียน Login</Button>
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
          หมายเหตุ: ในเฟสนี้ Mock Link ใช้งานได้เฉพาะ Browser Profile และเครื่องเดียวกันเท่านั้น
          เนื่องจากยังไม่มี Backend และฐานข้อมูลจริง
        </p>
      </Card>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-room-muted">รายการ Mock Sessions</h2>
        {loading ? (
          <p className="text-sm text-room-muted">กำลังโหลด...</p>
        ) : (
          <SessionsTable sessions={sessions} origin={origin} />
        )}
      </section>
    </main>
  );
}

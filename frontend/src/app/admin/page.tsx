"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import * as api from "@/lib/api-client";
import type { TrainingSession } from "@/types/domain";
import { SessionsTable } from "@/components/admin/SessionsTable";
import { Button, buttonVariants } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { LoadingBlock } from "@/components/shared/LoadingBlock";

export default function AdminPage() {
  const [sessions, setSessions] = useState<TrainingSession[]>([]);
  const [origin, setOrigin] = useState("");
  const [loading, setLoading] = useState(true);
  const [resetting, setResetting] = useState(false);

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
    setResetting(true);
    try {
      await api.resetDemoData();
      await reload();
    } finally {
      setResetting(false);
    }
  }

  return (
    <main className="mx-auto flex max-w-4xl flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold">SupportRoom AI — Admin</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          จัดการบทเรียนและสร้างลิงก์ห้องสอนการใช้งานระบบสำหรับผู้ใช้
        </p>
      </div>

      <div className="flex flex-wrap gap-3">
        <Link href="/admin/lessons" className={buttonVariants({ variant: "secondary" })}>
          จัดการบทเรียน
        </Link>
        <Link href="/admin/documents" className={buttonVariants({ variant: "secondary" })}>
          คลังเอกสาร
        </Link>
        <Link href="/admin/sessions/new" className={buttonVariants()}>
          สร้างลิงก์การสอน
        </Link>
        <Button variant="ghost" onClick={handleReset} disabled={resetting}>
          {resetting ? (
            <>
              <Spinner data-icon="inline-start" />
              กำลังรีเซ็ต...
            </>
          ) : (
            "Reset Demo Data"
          )}
        </Button>
      </div>

      <section className="flex flex-col gap-3">
        <h2 className="text-sm font-semibold tracking-wide text-muted-foreground uppercase">รายการ Sessions</h2>
        {loading ? (
          <LoadingBlock label="กำลังโหลดรายการ Session..." />
        ) : (
          <SessionsTable sessions={sessions} origin={origin} />
        )}
      </section>
    </main>
  );
}

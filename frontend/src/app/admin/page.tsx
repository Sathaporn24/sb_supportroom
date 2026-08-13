"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import * as api from "@/lib/api-client";
import type { TrainingLink } from "@/types/domain";
import { TrainingLinksTable } from "@/components/admin/TrainingLinksTable";
import { Button } from "@/components/ui/Button";
import { LoadingBlock } from "@/components/ui/LoadingBlock";
import { Spinner } from "@/components/ui/Spinner";

export default function AdminPage() {
  const [links, setLinks] = useState<TrainingLink[]>([]);
  const [origin, setOrigin] = useState("");
  const [loading, setLoading] = useState(true);
  const [resetting, setResetting] = useState(false);

  async function reload() {
    setLoading(true);
    const { links: list } = await api.listTrainingLinks();
    setLinks(list);
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
    <main className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-room-text">SupportRoom AI — Admin</h1>
        <p className="mt-1 text-sm text-room-muted">
          จัดการบทเรียนและสร้างลิงก์ห้องสอนการใช้งานระบบสำหรับผู้ใช้
        </p>
      </div>

      <div className="flex flex-wrap gap-3">
        <Link href="/admin/lessons">
          <Button variant="secondary">จัดการบทเรียน</Button>
        </Link>
        <Link href="/admin/documents">
          <Button variant="secondary">คลังเอกสาร</Button>
        </Link>
        <Link href="/admin/links/new">
          <Button>สร้างลิงก์การเรียน</Button>
        </Link>
        <Button variant="ghost" onClick={handleReset} disabled={resetting}>
          {resetting ? (
            <>
              <Spinner className="h-4 w-4" />
              กำลังรีเซ็ต...
            </>
          ) : (
            "Reset Demo Data"
          )}
        </Button>
      </div>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-room-muted">รายการลิงก์</h2>
        {loading ? <LoadingBlock label="กำลังโหลดรายการลิงก์..." /> : <TrainingLinksTable links={links} origin={origin} />}
      </section>
    </main>
  );
}

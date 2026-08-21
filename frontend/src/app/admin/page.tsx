"use client";

import { useEffect, useState } from "react";
import { LinkIcon, PlusIcon } from "lucide-react";
import { AdminLink } from "@/components/admin/AdminLink";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { LessonConfig, TrainingLink } from "@/types/domain";
import { TrainingLinksTable } from "@/components/admin/TrainingLinksTable";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { useAdminReviewCounts } from "@/hooks/use-admin-review-counts";
import { Button, buttonVariants } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { TableSkeleton } from "@/components/shared/TableSkeleton";
import { OnboardingChecklist } from "@/components/admin/OnboardingChecklist";

export default function AdminPage() {
  const { user } = useAdminSession();
  const { queueCount, conflictCount } = useAdminReviewCounts();
  const [links, setLinks] = useState<TrainingLink[]>([]);
  const [lessons, setLessons] = useState<LessonConfig[]>([]);
  const [origin, setOrigin] = useState("");
  const [loading, setLoading] = useState(true);
  const [resetting, setResetting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    setLoading(true);
    try {
      const [{ links: list }, { lessons: lessonList }] = await Promise.all([
        api.listTrainingLinks(),
        api.listLessons(),
      ]);
      setLinks(list);
      setLessons(lessonList);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "โหลดข้อมูลไม่สำเร็จ");
    } finally {
      setLoading(false);
    }
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
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold">ลิงก์การเรียน</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            สร้างและติดตามลิงก์ห้องสอนการใช้งานระบบที่ส่งให้ผู้ใช้
          </p>
        </div>
        {user?.role === "owner" && (
          <Button variant="ghost" size="sm" onClick={handleReset} disabled={resetting}>
            {resetting ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังรีเซ็ต...
              </>
            ) : (
              "Reset Demo Data"
            )}
          </Button>
        )}
      </div>

      <div className="flex flex-wrap gap-3">
        <AdminLink href="/admin/lessons/new" className={buttonVariants({ variant: "secondary" })}>
          <PlusIcon data-icon="inline-start" />
          เพิ่มบทเรียน
        </AdminLink>
        <AdminLink href="/admin/links/new" className={buttonVariants()}>
          <LinkIcon data-icon="inline-start" />
          สร้างลิงก์การเรียน
        </AdminLink>
      </div>

      <div className="flex flex-wrap gap-3">
        <AdminLink
          href="/admin/qna-queue"
          className={buttonVariants({ variant: "outline", size: "sm" })}
        >
          {queueCount} คำถามรอคำตอบ
        </AdminLink>
        <AdminLink
          href="/admin/qna-conflicts"
          className={buttonVariants({ variant: "outline", size: "sm" })}
        >
          {conflictCount} Q&amp;A ขัดกับเอกสาร
        </AdminLink>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <section className="flex flex-col gap-3">
        <h2 className="text-sm font-semibold tracking-wide text-muted-foreground uppercase">รายการลิงก์</h2>
        {loading ? (
          <TableSkeleton columns={6} />
        ) : links.length === 0 ? (
          <OnboardingChecklist hasLesson={lessons.length > 0} />
        ) : (
          <TrainingLinksTable links={links} origin={origin} />
        )}
      </section>
    </main>
  );
}

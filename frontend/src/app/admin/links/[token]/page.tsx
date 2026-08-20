"use client";

import { useEffect, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { learningSessionStatusLabel, linkStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import type { LearningSession, TrainingLink } from "@/types/domain";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TableSkeleton } from "@/components/shared/TableSkeleton";

const STAT_FIELD_COUNT = 6;

/** Mirrors this page's own layout (header, stat card, learner table) so the loading state doesn't
 * jump once the link and its sessions arrive. */
function TrainingLinkDetailSkeleton() {
  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div className="flex flex-col gap-2">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="h-6 w-56" />
        <Skeleton className="h-4 w-40" />
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-4">
          {Array.from({ length: STAT_FIELD_COUNT }).map((_, index) => (
            <div key={index} className="flex flex-col gap-1">
              <Skeleton className="h-3 w-20" />
              <Skeleton className="h-4 w-16" />
            </div>
          ))}
        </CardContent>
      </Card>

      <section className="flex flex-col gap-3">
        <Skeleton className="h-3 w-20" />
        <TableSkeleton columns={6} />
      </section>
    </main>
  );
}

/**
 * "7/20" when the browser has told us how big the deck is, a bare slide number when it has not,
 * and a dash for someone who opened the link but never got a slide in - three genuinely different
 * states that a single "สไลด์ที่ N" hid behind a number.
 */
function formatProgress(session: LearningSession): string {
  if (session.lastSlideIndex === null) return "-";
  const current = session.lastSlideIndex + 1;
  return session.totalSlideCount ? `${current}/${session.totalSlideCount}` : `สไลด์ที่ ${current}`;
}

/**
 * Everyone who has opened one link. This page used to be a single session's summary - after the
 * link/session split there is no longer "the" session behind a token, so the summary moved to
 * /admin/learning-sessions/[id] and this became the list that leads there.
 */
export default function TrainingLinkDetailPage() {
  const params = useParams<{ token: string }>();
  const [link, setLink] = useState<TrainingLink | null | "loading">("loading");
  const [lessonTitle, setLessonTitle] = useState("");
  const [sessions, setSessions] = useState<LearningSession[]>([]);

  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const { link: found, lessonTitle: title } = await api.getAdminTrainingLinkByToken(params.token);
        if (!active) return;
        setLink(found);
        setLessonTitle(title);
        const { learningSessions } = await api.listLearningSessionsForLink(found.id);
        if (active) setSessions(learningSessions);
      } catch {
        if (active) setLink(null);
      }
    })();
    return () => {
      active = false;
    };
  }, [params.token]);

  if (link === "loading") {
    return <TrainingLinkDetailSkeleton />;
  }
  if (!link) {
    return <main className="p-6 text-muted-foreground">ไม่พบลิงก์นี้ค่ะ</main>;
  }

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <AdminLink href="/admin" className="text-xs text-muted-foreground hover:text-foreground">
          ← กลับหน้า Admin
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold">{lessonTitle}</h1>
        <p className="text-sm text-muted-foreground">{link.recipientOrgName || "ไม่ระบุหน่วยงาน"}</p>
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-4 text-sm">
          <div className="flex flex-col items-start gap-1">
            <p className="text-muted-foreground">สถานะลิงก์</p>
            <Badge variant={link.status === "ACTIVE" ? "default" : "destructive"}>
              {linkStatusLabels[link.status]}
            </Badge>
          </div>
          <div>
            <p className="text-muted-foreground">หมดอายุ</p>
            <p>{formatDateTimeTh(link.expiresAt)}</p>
          </div>
          <div>
            <p className="text-muted-foreground">สร้างเมื่อ</p>
            <p>{formatDateTimeTh(link.createdAt)}</p>
          </div>
          <div>
            <p className="text-muted-foreground">ผู้เรียนทั้งหมด</p>
            <p>{link.learnerCount} คน</p>
          </div>
          <div>
            <p className="text-muted-foreground">กำลังเรียน</p>
            <p>{link.inProgressCount} รอบ</p>
          </div>
          <div>
            <p className="text-muted-foreground">จบแล้ว</p>
            <p>{link.endedCount} รอบ</p>
          </div>
        </CardContent>
      </Card>

      <section className="flex flex-col gap-3">
        <h2 className="text-sm font-semibold tracking-wide text-muted-foreground uppercase">ผู้เข้าเรียน</h2>
        {sessions.length === 0 ? (
          <p className="rounded-xl border border-dashed p-6 text-center text-sm text-muted-foreground">
            ยังไม่มีใครเปิดลิงก์นี้
          </p>
        ) : (
          <div className="overflow-hidden rounded-xl border">
            <Table className="min-w-[640px]">
              <TableHeader>
                <TableRow>
                  <TableHead className="px-4">ชื่อ</TableHead>
                  <TableHead className="px-4">เริ่มเรียน</TableHead>
                  <TableHead className="px-4">ความคืบหน้า</TableHead>
                  <TableHead className="px-4">คำถาม</TableHead>
                  <TableHead className="px-4">สถานะ</TableHead>
                  <TableHead className="px-4" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {sessions.map((session) => (
                  <TableRow key={session.id}>
                    <TableCell className="px-4 py-3 font-medium">{session.recipientName}</TableCell>
                    <TableCell className="px-4 py-3">{formatDateTimeTh(session.startedAt)}</TableCell>
                    <TableCell className="px-4 py-3">{formatProgress(session)}</TableCell>
                    <TableCell className="px-4 py-3">{session.questionCount}</TableCell>
                    <TableCell className="px-4 py-3">
                      <Badge
                        variant={
                          session.isStalled ? "destructive" : session.status === "IN_PROGRESS" ? "default" : "secondary"
                        }
                      >
                        {learningSessionStatusLabel(session)}
                      </Badge>
                    </TableCell>
                    <TableCell className="px-4 py-3">
                      <AdminLink
                        href={`/admin/learning-sessions/${session.id}`}
                        className={buttonVariants({ variant: "outline", size: "sm" })}
                      >
                        ดูสรุป
                      </AdminLink>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </section>
    </main>
  );
}

"use client";

import { useCallback, useEffect, useState } from "react";
import { RotateCcwIcon, Trash2Icon } from "lucide-react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { AdminRole, LessonTrashItem } from "@/types/domain";
import { getLessonTrashBadge } from "@/components/admin/lesson-trash-display";
import { LessonPermanentDeleteDialog } from "@/components/admin/LessonPermanentDeleteDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TableSkeleton } from "@/components/shared/TableSkeleton";
import { formatDateTimeTh } from "@/utils/format";

type Props = {
  role: AdminRole;
  /** Bumped by the parent to force a reload after an archive elsewhere adds a row here. */
  refreshToken: number;
  /** LT-2/LT-3/LT-4 - after either action, the active list must also refresh (a just-restored
   * lesson must stop showing up here, and vice versa). */
  onLessonRestored: () => void;
};

const CAN_ARCHIVE_OR_RESTORE: ReadonlySet<AdminRole> = new Set(["owner", "admin"]);

/**
 * LT-7 - strictly read-only: no edit, upload, move/scope-change, document/Q&A table, or bulk
 * action. This is a list plus, per row, an urgency indicator and (owner/admin) restore or
 * (owner only) permanent-delete - nothing else. Never carry an active-lesson affordance in here.
 */
export function LessonTrashList({ role, refreshToken, onLessonRestored }: Props) {
  const [lessons, setLessons] = useState<LessonTrashItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [restoringId, setRestoringId] = useState<string | null>(null);
  const [deleteDialogLesson, setDeleteDialogLesson] = useState<LessonTrashItem | null>(null);

  const reload = useCallback(async () => {
    setError(null);
    try {
      const { lessons: list } = await api.listTrashedLessons();
      setLessons(list);
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.response.error.message : "โหลดถังขยะบทเรียนไม่สำเร็จ");
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload, refreshToken]);

  async function handleRestore(lesson: LessonTrashItem) {
    setRestoringId(lesson.id);
    setError(null);
    try {
      await api.restoreLesson(lesson.id);
      await reload();
      onLessonRestored();
    } catch (caught) {
      // LT-13 - 409 here means the worker already claimed the purge; the row will show
      // "purging" on reload instead of restoring, which the error message above explains.
      setError(caught instanceof ApiClientError ? caught.response.error.message : "กู้คืนบทเรียนไม่สำเร็จ");
    } finally {
      setRestoringId(null);
    }
  }

  if (!lessons) {
    return <TableSkeleton columns={4} />;
  }

  return (
    <div className="flex flex-col gap-3">
      {error && <p className="text-xs text-destructive">{error}</p>}

      {lessons.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ไม่มีบทเรียนในถังขยะ</EmptyTitle>
            <EmptyDescription>บทเรียนที่ย้ายไปถังขยะจะแสดงที่นี่</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[720px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">ชื่อบทเรียน</TableHead>
                <TableHead className="px-4">ย้ายไปถังขยะเมื่อ</TableHead>
                <TableHead className="px-4">กำหนดลบถาวร</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4">จัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {lessons.map((lesson) => {
                const badge = getLessonTrashBadge(lesson.purgeState, lesson.urgency, lesson.remainingDays);
                return (
                  <TableRow key={lesson.id} data-testid={`lesson-trash-row-${lesson.id}`}>
                    <TableCell className="px-4 py-3 font-medium">{lesson.title}</TableCell>
                    <TableCell className="px-4 py-3 text-muted-foreground">
                      {formatDateTimeTh(lesson.deletedAt)}
                    </TableCell>
                    <TableCell className="px-4 py-3 text-muted-foreground">
                      {formatDateTimeTh(lesson.scheduledPurgeAt)}
                    </TableCell>
                    <TableCell className="px-4 py-3">
                      <Badge
                        variant={badge.variant}
                        className={badge.className}
                        data-testid={`lesson-trash-row-${lesson.id}-status-badge`}
                      >
                        {badge.label}
                      </Badge>
                    </TableCell>
                    <TableCell className="px-4 py-3">
                      {badge.disableActions ? (
                        <span className="text-xs text-muted-foreground">ไม่มีการดำเนินการ</span>
                      ) : (
                        <div className="flex items-center gap-2">
                          {CAN_ARCHIVE_OR_RESTORE.has(role) && (
                            <Button
                              type="button"
                              variant="secondary"
                              size="sm"
                              disabled={restoringId === lesson.id}
                              onClick={() => void handleRestore(lesson)}
                              data-testid={`lesson-trash-row-${lesson.id}-restore-button`}
                            >
                              {restoringId === lesson.id ? <Spinner data-icon="inline-start" /> : <RotateCcwIcon data-icon="inline-start" />}
                              กู้คืน
                            </Button>
                          )}
                          {role === "owner" && (
                            <Button
                              type="button"
                              variant="destructive"
                              size="sm"
                              onClick={() => setDeleteDialogLesson(lesson)}
                              data-testid={`lesson-trash-row-${lesson.id}-permanent-delete-button`}
                            >
                              <Trash2Icon data-icon="inline-start" />
                              ลบถาวร
                            </Button>
                          )}
                        </div>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}

      <LessonPermanentDeleteDialog
        lesson={deleteDialogLesson}
        onClose={() => setDeleteDialogLesson(null)}
        onQueued={() => void reload()}
      />
    </div>
  );
}

"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory, KnowledgeQnA, KnowledgeQnAFilter, LessonConfig } from "@/types/domain";
import { formatDateTimeTh } from "@/utils/format";
import { AdminLink } from "@/components/admin/AdminLink";
import { KnowledgeQnAAnswerDialog } from "@/components/admin/KnowledgeQnAAnswerDialog";
import { failureReasonLabels, scopeLabel, statusLabels, statusVariant } from "@/components/admin/DocumentUploadList";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TableSkeleton } from "@/components/shared/TableSkeleton";

const TABLE_COLUMN_COUNT = 6;

type Props = {
  filter: KnowledgeQnAFilter;
  categories: KnowledgeCategory[];
  lessons: LessonConfig[];
};

/**
 * KL-1/KL-8 - the Q&A half of the library page, driven by the same filter+search bar as the
 * documents table above it. KL-14/KL-15/KL-16 - the only place in the system that lets CS edit or
 * delete an already-saved KnowledgeQnA row.
 */
export function KnowledgeQnATable({ filter, categories, lessons }: Props) {
  const [qnas, setQnas] = useState<KnowledgeQnA[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [editingQnA, setEditingQnA] = useState<KnowledgeQnA | null>(null);
  const [deletingQnA, setDeletingQnA] = useState<KnowledgeQnA | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [showDeletedNotice, setShowDeletedNotice] = useState(false);

  async function reload() {
    try {
      const { qnas: list } = await api.listKnowledgeQnA(filter);
      setQnas(list);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "โหลดคำถาม-คำตอบไม่สำเร็จ");
    }
  }

  useEffect(() => {
    void reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter.scopeType, filter.scopeId, filter.q]);

  function handleEditSaved() {
    setEditingQnA(null);
    void reload();
  }

  async function handleDeleteConfirm() {
    if (!deletingQnA) return;
    setDeleting(true);
    setDeleteError(null);
    try {
      await api.deleteKnowledgeQnA(deletingQnA.id);
      setDeletingQnA(null);
      setShowDeletedNotice(true);
      await reload();
    } catch (err) {
      setDeleteError(err instanceof ApiClientError ? err.response.error.message : "ลบไม่สำเร็จ");
    } finally {
      setDeleting(false);
    }
  }

  return (
    <div className="flex flex-col gap-3">
      {/* KL-16 - the deletion's effect on the review queue has to be visible right here, not just
          true somewhere in the backend. */}
      {showDeletedNotice && (
        <Alert data-testid="qna-delete-success-alert">
          <AlertTitle>ลบคำถาม-คำตอบแล้ว</AlertTitle>
          <AlertDescription>
            คำถามที่เคยปิดไว้กลับเข้าคิวรีวิวแล้ว —{" "}
            <AdminLink href="/admin/qna-queue">ไปดูคิวคำถามที่ยังไม่มีคำตอบ</AdminLink>
          </AlertDescription>
        </Alert>
      )}

      {error && <p className="text-sm text-destructive">{error}</p>}

      {!qnas ? (
        <TableSkeleton columns={TABLE_COLUMN_COUNT} />
      ) : qnas.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ยังไม่มีคำถาม-คำตอบ</EmptyTitle>
            <EmptyDescription>เขียนคำตอบจากคิวคำถามที่ยังไม่มีคำตอบได้ที่หน้า &quot;คิวคำถาม&quot;</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[720px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">คำถาม</TableHead>
                <TableHead className="px-4">คำตอบ</TableHead>
                <TableHead className="px-4">ขอบเขต</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4">บันทึกเมื่อ</TableHead>
                <TableHead className="px-4">จัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {qnas.map((qna) => (
                <TableRow key={qna.id} data-testid={`qna-row-${qna.id}`}>
                  <TableCell className="px-4 py-3">{qna.question}</TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{qna.answer}</TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">
                    {scopeLabel(qna.scopeType, qna.scopeId, categories, lessons)}
                  </TableCell>
                  <TableCell className="px-4 py-3">
                    <Badge variant={statusVariant[qna.indexingStatus]}>{statusLabels[qna.indexingStatus]}</Badge>
                    {qna.indexingStatus === "failed" && (
                      <p className="mt-1 text-xs whitespace-normal text-muted-foreground">
                        {qna.failureReason ? failureReasonLabels[qna.failureReason] : "เกิดข้อผิดพลาดที่ระบุสาเหตุไม่ได้"}
                      </p>
                    )}
                  </TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{formatDateTimeTh(qna.createdAt)}</TableCell>
                  <TableCell className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setEditingQnA(qna)}
                        data-testid={`qna-row-${qna.id}-edit-button`}
                      >
                        แก้ไข
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                          setDeleteError(null);
                          setDeletingQnA(qna);
                        }}
                        data-testid={`qna-row-${qna.id}-delete-button`}
                      >
                        ลบ
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {editingQnA && (
        <KnowledgeQnAAnswerDialog
          mode="edit"
          open={editingQnA !== null}
          qna={editingQnA}
          onClose={() => setEditingQnA(null)}
          onSaved={handleEditSaved}
        />
      )}

      {/* KL-15 - both side effects of deleting a Q&A have to be spelled out, not a bare
          "ต้องการลบใช่หรือไม่". */}
      <AlertDialog open={deletingQnA !== null} onOpenChange={(next) => !next && setDeletingQnA(null)}>
        <AlertDialogContent data-testid="qna-delete-confirm-dialog">
          <AlertDialogHeader>
            <AlertDialogTitle>ลบคำถาม-คำตอบนี้ใช่หรือไม่</AlertDialogTitle>
            <AlertDialogDescription>
              คำถามที่ Q&amp;A นี้เคยปิดไว้จะกลับเข้าคิวรีวิวอีกครั้ง และข้อมูลจะถูกลบออกจากคลังความรู้ AI จะเลิกใช้ตอบ
            </AlertDialogDescription>
          </AlertDialogHeader>
          {deleteError && <p className="text-xs text-destructive">{deleteError}</p>}
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleting} data-testid="qna-delete-cancel-button">
              ยกเลิก
            </AlertDialogCancel>
            <Button
              variant="destructive"
              onClick={handleDeleteConfirm}
              disabled={deleting}
              data-testid="qna-delete-confirm-button"
            >
              {deleting ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังลบ...
                </>
              ) : (
                "ลบ"
              )}
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

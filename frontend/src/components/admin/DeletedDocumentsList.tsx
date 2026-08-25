"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { DocumentResource } from "@/types/domain";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TableSkeleton } from "@/components/shared/TableSkeleton";

/** R6.1/DI-15 - deleted documents are soft-deleted only (the DB row and file are kept), so they
 * can be brought back. Restoring re-extracts and re-indexes from scratch, spending embedding cost
 * again - the row is not "un-deleted" for free. */
export function DeletedDocumentsList() {
  const [documents, setDocuments] = useState<DocumentResource[] | null>(null);
  const [restoringId, setRestoringId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    const { documents: list } = await api.listDeletedDocuments();
    setDocuments(list);
  }

  useEffect(() => {
    void reload();
  }, []);

  async function handleRestore(id: string) {
    setRestoringId(id);
    setError(null);
    try {
      await api.restoreDocument(id);
      await reload();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "กู้คืนเอกสารไม่สำเร็จ");
    } finally {
      setRestoringId(null);
    }
  }

  if (!documents) {
    return <TableSkeleton columns={4} />;
  }

  return (
    <div className="flex flex-col gap-3">
      {error && <p className="text-xs text-destructive">{error}</p>}

      {documents.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ไม่มีเอกสารที่ถูกลบ</EmptyTitle>
            <EmptyDescription>เอกสารที่ถูกลบจะแสดงที่นี่ กู้คืนได้ตลอดเวลา</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[560px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">ไฟล์</TableHead>
                <TableHead className="px-4">อัปโหลดเมื่อ</TableHead>
                <TableHead className="px-4">สถานะการลบ</TableHead>
                <TableHead className="px-4">จัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {documents.map((doc) => (
                <TableRow key={doc.id} data-testid={`deleted-document-row-${doc.id}`}>
                  <TableCell className="px-4 py-3">{doc.fileName}</TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{formatDateTimeTh(doc.createdAt)}</TableCell>
                  <TableCell className="px-4 py-3">
                    {doc.hasPendingVectorDelete ? (
                      <Badge variant="secondary">กำลังลบข้อมูลออกจากคลังความรู้...</Badge>
                    ) : (
                      <Badge variant="outline">ลบออกจากคลังความรู้แล้ว</Badge>
                    )}
                  </TableCell>
                  <TableCell className="px-4 py-3">
                    <Button
                      data-testid={`deleted-document-row-${doc.id}-restore-button`}
                      variant="secondary"
                      size="sm"
                      onClick={() => handleRestore(doc.id)}
                      disabled={restoringId === doc.id}
                    >
                      {restoringId === doc.id ? <Spinner /> : "กู้คืน"}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}

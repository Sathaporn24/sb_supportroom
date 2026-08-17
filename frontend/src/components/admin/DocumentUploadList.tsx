"use client";

import { useEffect, useRef, useState, type ChangeEvent } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { DocumentIndexingStatus, DocumentResource } from "@/types/domain";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { LoadingBlock } from "@/components/shared/LoadingBlock";

const statusVariant = {
  pending: "outline",
  indexed: "default",
  failed: "destructive",
} as const;

const statusLabels: Record<DocumentIndexingStatus, string> = {
  pending: "กำลังประมวลผล",
  indexed: "พร้อมใช้งาน",
  failed: "อ่านไม่สำเร็จ",
};

function formatSize(bytes: number): string {
  if (bytes < 1024 * 1024) {
    return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** Upload + list + delete for CS-uploaded documents (.pptx/.pdf/.docx/.xlsx). Omit lessonSlug for
 * the standalone/global library; pass it to scope this to one lesson's attached documents. Pass
 * primaryDocumentId (the lesson's pdfDocumentResourceId) when this list sits next to a PDF-source
 * lesson editor, so the one document actually driving the slides is visually distinguished from
 * plain Q&A-only attachments - otherwise the two look identical and "which file is my slides?"
 * is unanswerable from this table alone. */
export function DocumentUploadList({ lessonSlug, primaryDocumentId }: { lessonSlug?: string; primaryDocumentId?: string }) {
  const [documents, setDocuments] = useState<DocumentResource[] | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  async function reload() {
    const { documents: list } = await api.listDocuments(lessonSlug);
    setDocuments(list);
  }

  useEffect(() => {
    void reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lessonSlug]);

  async function handleFileSelected(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) {
      return;
    }
    setUploading(true);
    setError(null);
    try {
      await api.uploadDocument(file, lessonSlug);
      await reload();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "อัปโหลดไม่สำเร็จ");
    } finally {
      setUploading(false);
    }
  }

  async function handleDelete(id: string) {
    const confirmed = window.confirm("ต้องการลบเอกสารนี้ใช่หรือไม่?");
    if (!confirmed) {
      return;
    }
    setDeletingId(id);
    try {
      await api.deleteDocument(id);
      await reload();
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-xs text-muted-foreground">
          รองรับ .pptx, .pdf, .docx, .xlsx — ใช้เอกสารเดิมที่มีอยู่แล้วได้เลย ไม่ต้องทำใหม่
        </p>
        <div>
          <input
            ref={inputRef}
            type="file"
            accept=".pptx,.pdf,.docx,.xlsx"
            className="hidden"
            onChange={handleFileSelected}
          />
          <Button variant="secondary" onClick={() => inputRef.current?.click()} disabled={uploading}>
            {uploading ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังอัปโหลด...
              </>
            ) : (
              "อัปโหลดเอกสาร"
            )}
          </Button>
        </div>
      </div>

      {error && <p className="text-xs text-destructive">{error}</p>}

      {!documents ? (
        <LoadingBlock label="กำลังโหลดรายการเอกสาร..." />
      ) : documents.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ยังไม่มีเอกสาร</EmptyTitle>
            <EmptyDescription>ลองอัปโหลดไฟล์เดิมที่มีอยู่แล้วได้เลยค่ะ</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[560px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">ไฟล์</TableHead>
                <TableHead className="px-4">ขนาด</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4">อัปโหลดเมื่อ</TableHead>
                <TableHead className="px-4">จัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {documents.map((doc) => (
                <TableRow key={doc.id}>
                  <TableCell className="px-4 py-3">
                    {doc.fileName}
                    {doc.id === primaryDocumentId && (
                      <Badge variant="secondary" className="ml-2">
                        ใช้เป็นสไลด์หลัก
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{formatSize(doc.sizeBytes)}</TableCell>
                  <TableCell className="px-4 py-3">
                    <Badge variant={statusVariant[doc.indexingStatus]}>
                      {statusLabels[doc.indexingStatus]}
                      {doc.indexingStatus === "indexed" ? ` (${doc.indexedChunkCount} chunk)` : ""}
                    </Badge>
                    {doc.indexingStatus === "failed" && (
                      <p className="mt-1 text-xs whitespace-normal text-muted-foreground">
                        อาจเป็นไฟล์สแกน/รูปภาพที่ไม่มีข้อความให้อ่าน ลองส่งออกไฟล์ใหม่แบบมีข้อความจริง
                      </p>
                    )}
                  </TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{formatDateTimeTh(doc.createdAt)}</TableCell>
                  <TableCell className="px-4 py-3">
                    <Button variant="ghost" size="sm" onClick={() => handleDelete(doc.id)} disabled={deletingId === doc.id}>
                      {deletingId === doc.id ? <Spinner /> : "ลบ"}
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

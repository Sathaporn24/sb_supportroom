"use client";

import { useEffect, useRef, useState, type ChangeEvent } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { DocumentIndexingStatus, DocumentResource } from "@/types/domain";
import { formatDateTimeTh } from "@/utils/format";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { LoadingBlock } from "@/components/ui/LoadingBlock";
import { Spinner } from "@/components/ui/Spinner";

const statusTone = {
  pending: "warning",
  indexed: "success",
  failed: "danger",
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
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-xs text-room-muted">
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
                <Spinner className="h-4 w-4" />
                กำลังอัปโหลด...
              </>
            ) : (
              "อัปโหลดเอกสาร"
            )}
          </Button>
        </div>
      </div>

      {error && <p className="text-xs text-red-600">{error}</p>}

      {!documents ? (
        <LoadingBlock label="กำลังโหลดรายการเอกสาร..." />
      ) : documents.length === 0 ? (
        <p className="rounded-xl border border-dashed border-room-border p-6 text-center text-sm text-room-muted">
          ยังไม่มีเอกสาร ลองอัปโหลดไฟล์เดิมที่มีอยู่แล้วได้เลยค่ะ
        </p>
      ) : (
        <div className="overflow-x-auto rounded-xl border border-room-border">
          <table className="w-full min-w-[560px] text-left text-sm">
            <thead className="bg-room-panelAlt text-xs uppercase tracking-wide text-room-muted">
              <tr>
                <th className="px-4 py-3">ไฟล์</th>
                <th className="px-4 py-3">ขนาด</th>
                <th className="px-4 py-3">สถานะ</th>
                <th className="px-4 py-3">อัปโหลดเมื่อ</th>
                <th className="px-4 py-3">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {documents.map((doc) => (
                <tr key={doc.id} className="border-t border-room-border">
                  <td className="px-4 py-3 text-room-text">
                    {doc.fileName}
                    {doc.id === primaryDocumentId && (
                      <Badge tone="info" className="ml-2">
                        ใช้เป็นสไลด์หลัก
                      </Badge>
                    )}
                  </td>
                  <td className="px-4 py-3 text-room-muted">{formatSize(doc.sizeBytes)}</td>
                  <td className="px-4 py-3">
                    <Badge tone={statusTone[doc.indexingStatus]}>
                      {statusLabels[doc.indexingStatus]}
                      {doc.indexingStatus === "indexed" ? ` (${doc.indexedChunkCount} chunk)` : ""}
                    </Badge>
                    {doc.indexingStatus === "failed" && (
                      <p className="mt-1 text-xs text-room-muted">
                        อาจเป็นไฟล์สแกน/รูปภาพที่ไม่มีข้อความให้อ่าน ลองส่งออกไฟล์ใหม่แบบมีข้อความจริง
                      </p>
                    )}
                  </td>
                  <td className="px-4 py-3 text-room-muted">{formatDateTimeTh(doc.createdAt)}</td>
                  <td className="px-4 py-3">
                    <Button variant="ghost" onClick={() => handleDelete(doc.id)} disabled={deletingId === doc.id}>
                      {deletingId === doc.id ? <Spinner className="h-4 w-4" /> : "ลบ"}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

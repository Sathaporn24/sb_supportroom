"use client";

import { useEffect, useMemo, useRef, useState, type ChangeEvent } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type {
  DocumentFailureReason,
  DocumentIndexingStatus,
  DocumentResource,
  DocumentScope,
  KnowledgeCategory,
} from "@/types/domain";
import { formatDateTimeTh } from "@/utils/format";
import { AdminLink } from "@/components/admin/AdminLink";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
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

/** R6.4 - each failure reason needs a different fix from CS, so these must never collapse into
 * one generic message (mirrors DocumentFailureReason.cs). */
const failureReasonLabels: Record<DocumentFailureReason, string> = {
  unsupported_type: "ไฟล์ประเภทนี้ไม่รองรับ — ลองส่งออกเป็น .pptx, .pdf, .docx หรือ .xlsx",
  extract_failed: "แปลงไฟล์เป็นข้อความไม่สำเร็จ — ลองส่งออกไฟล์ใหม่",
  no_text: "ไม่พบข้อความในไฟล์ — อาจเป็นไฟล์สแกน/รูปภาพ ลองส่งออกไฟล์ใหม่แบบมีข้อความจริง",
  embedding_failed: "แปลงข้อความเป็นเวกเตอร์ไม่สำเร็จ — ระบบจะลองใหม่ให้อัตโนมัติ",
  index_failed: "บันทึกเข้าคลังความรู้ไม่สำเร็จ — ระบบจะลองใหม่ให้อัตโนมัติ",
};

function formatSize(bytes: number): string {
  if (bytes < 1024 * 1024) {
    return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** DS-8 - "ทั้งบริษัท / เฉพาะหมวด" only, no "เฉพาะบทเรียน" option: this picker only ever appears on
 * the library page, where scope is either company-wide or one Level-2 category. Same shape as
 * KnowledgeQnAAnswerDialog's scope fields, kept to one implementation in this file and reused by
 * both the upload form and the per-row move dialog below. */
function CompanyOrCategoryScopeFields({
  scopeType,
  scopeId,
  categories,
  onChange,
}: {
  scopeType: "company" | "category";
  scopeId: string | undefined;
  categories: KnowledgeCategory[];
  onChange: (next: DocumentScope) => void;
}) {
  const subcategories = categories
    .filter((c) => c.level === 2)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));
  const parentsById = new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c]));

  return (
    <div className="flex flex-col gap-2">
      <RadioGroup
        value={scopeType}
        onValueChange={(value) => onChange({ scopeType: value as "company" | "category", scopeId: undefined })}
        className="flex flex-row gap-4"
      >
        <Label className="font-normal">
          <RadioGroupItem value="company" />
          ทั้งบริษัท
        </Label>
        <Label className="font-normal">
          <RadioGroupItem value="category" />
          เฉพาะหมวด
        </Label>
      </RadioGroup>
      {scopeType === "category" && (
        <Select
          value={scopeId ?? ""}
          onValueChange={(value) => value && onChange({ scopeType: "category", scopeId: value })}
        >
          <SelectTrigger className="w-full">
            <SelectValue placeholder="เลือกหมวด" />
          </SelectTrigger>
          <SelectContent>
            {subcategories.map((sub) => (
              <SelectItem key={sub.id} value={sub.id}>
                {parentsById.get(sub.parentId ?? "")?.name ?? "?"} › {sub.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
    </div>
  );
}

function scopeLabel(doc: DocumentResource, categories: KnowledgeCategory[]): string {
  if (doc.scopeType === "company") {
    return "ทั้งบริษัท";
  }
  if (doc.scopeType === "lesson") {
    return "บทเรียนนี้";
  }
  const category = categories.find((c) => c.id === doc.scopeId);
  const parent = category ? categories.find((c) => c.id === category.parentId) : undefined;
  return category ? (parent ? `${parent.name} › ${category.name}` : category.name) : "หมวดที่ถูกลบไปแล้ว";
}

type Props =
  | { fixedScope: DocumentScope; primaryDocumentId?: string }
  | { fixedScope?: undefined; primaryDocumentId?: undefined };

/**
 * Upload + list + delete for CS-uploaded documents (.pptx/.pdf/.docx/.xlsx). Two modes:
 * - `fixedScope` set (embedded in a lesson editor, DS-8/Q-C): scope is locked, no picker, no
 *   filter, no scope column, no move UI - the page it sits on already fixes the context.
 * - `fixedScope` omitted (the `/admin/documents` library page): full scope picker on upload, a
 *   filter to browse by scope, a scope column per row, and a move-scope action per row (DS-5/DS-9).
 * `primaryDocumentId` (the lesson's pdfDocumentResourceId) only makes sense in fixed-scope mode -
 * it visually distinguishes the one document actually driving the slides from plain attachments.
 */
export function DocumentUploadList({ fixedScope, primaryDocumentId }: Props) {
  const libraryMode = !fixedScope;

  const [documents, setDocuments] = useState<DocumentResource[] | null>(null);
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const [uploadScope, setUploadScope] = useState<DocumentScope>({ scopeType: "company" });
  const [filterScope, setFilterScope] = useState<DocumentScope>({ scopeType: "company" });
  const [movingDoc, setMovingDoc] = useState<DocumentResource | null>(null);
  const [moveScope, setMoveScope] = useState<DocumentScope>({ scopeType: "company" });
  const [moving, setMoving] = useState(false);
  const [moveError, setMoveError] = useState<string | null>(null);

  const activeScope = fixedScope ?? filterScope;

  async function reload(scope: DocumentScope) {
    const { documents: list } = await api.listDocuments(scope);
    setDocuments(list);
  }

  useEffect(() => {
    void reload(activeScope);
    if (libraryMode) {
      void api.listKnowledgeCategories().then(({ categories: list }) => setCategories(list));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeScope.scopeType, activeScope.scopeId]);

  async function handleFileSelected(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) {
      return;
    }
    setUploading(true);
    setError(null);
    try {
      await api.uploadDocument(file, fixedScope ?? uploadScope);
      await reload(activeScope);
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
      await reload(activeScope);
    } finally {
      setDeletingId(null);
    }
  }

  function openMoveDialog(doc: DocumentResource) {
    setMovingDoc(doc);
    setMoveError(null);
    setMoveScope(
      doc.scopeType === "category" ? { scopeType: "category", scopeId: doc.scopeId } : { scopeType: "company" },
    );
  }

  async function handleMoveConfirm() {
    if (!movingDoc) {
      return;
    }
    setMoving(true);
    setMoveError(null);
    try {
      await api.moveDocumentScope(movingDoc.id, moveScope);
      setMovingDoc(null);
      await reload(activeScope);
    } catch (err) {
      setMoveError(err instanceof ApiClientError ? err.response.error.message : "ย้ายขอบเขตไม่สำเร็จ");
    } finally {
      setMoving(false);
    }
  }

  const canUpload = !libraryMode || uploadScope.scopeType === "company" || Boolean(uploadScope.scopeId);
  const canConfirmMove = moveScope.scopeType === "company" || Boolean(moveScope.scopeId);

  const filterSubcategories = useMemo(
    () =>
      categories
        .filter((c) => c.level === 2)
        .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th")),
    [categories],
  );
  const filterParentsById = useMemo(
    () => new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c])),
    [categories],
  );

  return (
    <div className="flex flex-col gap-3">
      {libraryMode && (
        <div className="flex flex-col gap-2 rounded-xl border p-3">
          <Label>ดูเอกสารในขอบเขต</Label>
          <div className="flex flex-wrap items-center gap-2">
            <Select
              value={filterScope.scopeType === "category" ? (filterScope.scopeId ?? "") : filterScope.scopeType}
              onValueChange={(value) => {
                if (!value) return;
                setFilterScope(value === "company" ? { scopeType: "company" } : { scopeType: "category", scopeId: value });
              }}
            >
              <SelectTrigger className="w-64">
                <SelectValue placeholder="เลือกขอบเขต" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="company">ทั้งบริษัท</SelectItem>
                {filterSubcategories.map((sub) => (
                  <SelectItem key={sub.id} value={sub.id}>
                    {filterParentsById.get(sub.parentId ?? "")?.name ?? "?"} › {sub.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
      )}

      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-xs text-muted-foreground">
          รองรับ .pptx, .pdf, .docx, .xlsx — ใช้เอกสารเดิมที่มีอยู่แล้วได้เลย ไม่ต้องทำใหม่
        </p>
        <div className="flex flex-col items-end gap-2">
          {libraryMode && (
            <div className="w-64">
              <CompanyOrCategoryScopeFields
                scopeType={uploadScope.scopeType as "company" | "category"}
                scopeId={uploadScope.scopeId}
                categories={categories}
                onChange={setUploadScope}
              />
            </div>
          )}
          <input
            ref={inputRef}
            type="file"
            accept=".pptx,.pdf,.docx,.xlsx"
            className="hidden"
            onChange={handleFileSelected}
          />
          <Button variant="secondary" onClick={() => inputRef.current?.click()} disabled={uploading || !canUpload}>
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
                {libraryMode && <TableHead className="px-4">ขอบเขต</TableHead>}
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
                  {libraryMode && (
                    <TableCell className="px-4 py-3 text-muted-foreground">{scopeLabel(doc, categories)}</TableCell>
                  )}
                  <TableCell className="px-4 py-3">
                    <Badge variant={statusVariant[doc.indexingStatus]}>
                      {statusLabels[doc.indexingStatus]}
                      {doc.indexingStatus === "indexed" ? ` (${doc.indexedChunkCount} chunk)` : ""}
                    </Badge>
                    {doc.indexingStatus === "failed" && (
                      <p className="mt-1 text-xs whitespace-normal text-muted-foreground">
                        {doc.failureReason ? failureReasonLabels[doc.failureReason] : "เกิดข้อผิดพลาดที่ระบุสาเหตุไม่ได้"}
                      </p>
                    )}
                    {/* DI-10 - a self-scheduled retry is a different situation from "needs a
                        person": keep it as its own line, never merged into the failure text
                        above. */}
                    {doc.willRetryAt && (
                      <p className="mt-1 text-xs whitespace-normal text-muted-foreground">
                        ระบบจะลองใหม่อัตโนมัติเมื่อ {formatDateTimeTh(doc.willRetryAt)}
                      </p>
                    )}
                  </TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{formatDateTimeTh(doc.createdAt)}</TableCell>
                  <TableCell className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      <AdminLink
                        href={`/admin/documents/${encodeURIComponent(doc.id)}/chunks?fileName=${encodeURIComponent(doc.fileName)}`}
                        className="text-xs text-primary hover:underline"
                      >
                        ดูข้อความที่แปลงได้
                      </AdminLink>
                      {libraryMode && (
                        <Button variant="ghost" size="sm" onClick={() => openMoveDialog(doc)}>
                          ย้ายขอบเขต
                        </Button>
                      )}
                      <Button variant="ghost" size="sm" onClick={() => handleDelete(doc.id)} disabled={deletingId === doc.id}>
                        {deletingId === doc.id ? <Spinner /> : "ลบ"}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={movingDoc !== null} onOpenChange={(next) => !next && setMovingDoc(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>ย้ายขอบเขต — {movingDoc?.fileName}</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <CompanyOrCategoryScopeFields
              scopeType={moveScope.scopeType as "company" | "category"}
              scopeId={moveScope.scopeId}
              categories={categories}
              onChange={setMoveScope}
            />
            {moveError && <p className="text-xs text-destructive">{moveError}</p>}
            <div className="flex justify-end gap-2">
              <Button variant="ghost" onClick={() => setMovingDoc(null)} disabled={moving}>
                ยกเลิก
              </Button>
              <Button onClick={handleMoveConfirm} disabled={!canConfirmMove || moving}>
                {moving ? (
                  <>
                    <Spinner data-icon="inline-start" />
                    กำลังย้าย...
                  </>
                ) : (
                  "ย้าย"
                )}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

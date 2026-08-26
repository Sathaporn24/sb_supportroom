"use client";

import { useEffect, useRef, useState, type ChangeEvent } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type {
  DocumentFailureReason,
  DocumentIndexingStatus,
  DocumentResource,
  DocumentScope,
  DuplicateDocumentsResponse,
  KnowledgeCategory,
  KnowledgeQnAFilter,
  KnowledgeScopeType,
  LessonConfig,
} from "@/types/domain";
import { formatDateTimeTh } from "@/utils/format";
import { AdminLink } from "@/components/admin/AdminLink";
import { DocumentDuplicateDialog } from "@/components/admin/DocumentDuplicateDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TableSkeleton } from "@/components/shared/TableSkeleton";

/** Shared with KnowledgeQnATable - IndexingStatus means the same thing for a document and a Q&A
 * row (both flow through the same background indexing job, DI-5). */
export const statusVariant = {
  pending: "outline",
  indexed: "default",
  failed: "destructive",
} as const;

export const statusLabels: Record<DocumentIndexingStatus, string> = {
  pending: "กำลังประมวลผล",
  indexed: "พร้อมใช้งาน",
  failed: "อ่านไม่สำเร็จ",
};

/** R6.4 - each failure reason needs a different fix from CS, so these must never collapse into
 * one generic message (mirrors DocumentFailureReason.cs). */
export const failureReasonLabels: Record<DocumentFailureReason, string> = {
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
 * the library page, where a document is uploaded as company-wide or into one Level-2 category
 * (attaching to a specific lesson is still done by moving it after upload, DS-5/DS-9). Same shape
 * as KnowledgeQnAAnswerDialog's scope fields, kept to one implementation in this file and reused
 * by both the upload form and the per-row move dialog below. */
function CompanyOrCategoryScopeFields({
  scopeType,
  scopeId,
  categories,
  onChange,
  testidPrefix,
}: {
  scopeType: "company" | "category";
  scopeId: string | undefined;
  categories: KnowledgeCategory[];
  onChange: (next: DocumentScope) => void;
  testidPrefix: string;
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
          <RadioGroupItem value="company" data-testid={`${testidPrefix}-company-radio`} />
          ทั้งบริษัท
        </Label>
        <Label className="font-normal">
          <RadioGroupItem value="category" data-testid={`${testidPrefix}-category-radio`} />
          เฉพาะหมวด
        </Label>
      </RadioGroup>
      {scopeType === "category" && (
        <Select
          value={scopeId ?? ""}
          onValueChange={(value) => value && onChange({ scopeType: "category", scopeId: value })}
        >
          <SelectTrigger className="w-full" data-testid={`${testidPrefix}-category-select`}>
            <SelectValue placeholder="เลือกหมวด">
              {(value: string) => {
                const sub = subcategories.find((c) => c.id === value);
                return sub ? `${parentsById.get(sub.parentId ?? "")?.name ?? "?"} › ${sub.name}` : "เลือกหมวด";
              }}
            </SelectValue>
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

/** KL-6 - reads out all three scope levels for a document/Q&A row. `lesson` shows the real lesson
 * title (not the literal string "บทเรียนนี้", which only ever made sense in the fixed-scope
 * embed) and an id that resolves to nothing shows a "ถูกลบไปแล้ว" label - never a raw id, never a
 * hidden row. Exported so the Q&A table and the duplicate-upload dialog (KL-22) use the exact same
 * wording instead of a second implementation. */
export function scopeLabel(
  scopeType: KnowledgeScopeType,
  scopeId: string | undefined,
  categories: KnowledgeCategory[],
  lessons: LessonConfig[],
): string {
  if (scopeType === "company") {
    return "ทั้งบริษัท";
  }
  if (scopeType === "lesson") {
    const lesson = lessons.find((l) => l.id === scopeId);
    return lesson ? lesson.title : "บทเรียนที่ถูกลบไปแล้ว";
  }
  const category = categories.find((c) => c.id === scopeId);
  const parent = category ? categories.find((c) => c.id === category.parentId) : undefined;
  return category ? (parent ? `${parent.name} › ${category.name}` : category.name) : "หมวดที่ถูกลบไปแล้ว";
}

/** KL-7 - the document driving a lesson's slides gets a badge wherever it shows up in the library,
 * built purely from the already-loaded lesson list (no new endpoint). Replaces the old badge that
 * only existed in the now-removed `fixedScope` mode (`primaryDocumentId`). */
function slideLessonFor(doc: DocumentResource, lessons: LessonConfig[]): LessonConfig | undefined {
  return lessons.find((l) => l.pdfDocumentResourceId === doc.id);
}

type Props = {
  filter: KnowledgeQnAFilter;
  categories: KnowledgeCategory[];
  lessons: LessonConfig[];
};

/**
 * Upload + list + delete for CS-uploaded documents (.pptx/.pdf/.docx/.xlsx), used only on the
 * `/admin/documents` library page (KL-1). `filter`/`categories`/`lessons` are owned by the page
 * (shared with the Q&A table below it) - a scope column per row, a move-scope action per row
 * (DS-5/DS-9), and KL-21's duplicate check on upload.
 *
 * UC-2 - this used to also support a `fixedScope` mode embedded directly in the lesson editor
 * (DS-8/Q-C); that mode was deleted entirely per CR-1.j, not just stopped being called - the
 * library page's KL-7 badge (below) now covers what its `primaryDocumentId` prop used to show.
 */
export function DocumentUploadList({ filter, categories, lessons }: Props) {
  const [documents, setDocuments] = useState<DocumentResource[] | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const [uploadScope, setUploadScope] = useState<DocumentScope>({ scopeType: "company" });
  const [movingDoc, setMovingDoc] = useState<DocumentResource | null>(null);
  const [moveScope, setMoveScope] = useState<DocumentScope>({ scopeType: "company" });
  const [moving, setMoving] = useState(false);
  const [moveError, setMoveError] = useState<string | null>(null);

  const [pendingUpload, setPendingUpload] = useState<{ file: File; scope: DocumentScope } | null>(null);
  const [duplicates, setDuplicates] = useState<DuplicateDocumentsResponse | null>(null);

  async function reload(scope: KnowledgeQnAFilter) {
    const { documents: list } = await api.listDocuments(scope);
    setDocuments(list);
  }

  useEffect(() => {
    void reload(filter);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter.scopeType, filter.scopeId, filter.q]);

  async function performUpload(file: File, scope: DocumentScope, checkDuplicate: boolean) {
    setUploading(true);
    setError(null);
    try {
      await api.uploadDocument(file, scope, checkDuplicate);
      await reload(filter);
      setPendingUpload(null);
      setDuplicates(null);
    } catch (err) {
      // KL-21 - a 409 here is a normal ApiClientError; the duplicate payload rides in
      // response.error.details rather than a distinct error type.
      if (err instanceof ApiClientError && err.status === 409) {
        setPendingUpload({ file, scope });
        setDuplicates(err.response.error.details as DuplicateDocumentsResponse);
        return;
      }
      setError(err instanceof ApiClientError ? err.response.error.message : "อัปโหลดไม่สำเร็จ");
    } finally {
      setUploading(false);
    }
  }

  async function handleFileSelected(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) {
      return;
    }
    // KL-21 - checkDuplicate=true only here, the library page's own upload form; LessonForm's
    // PDF upload (UC-5) calls api.uploadDocument directly and never sets it, so a lesson can
    // always reuse an existing file.
    await performUpload(file, uploadScope, true);
  }

  function handleUploadAnyway() {
    if (!pendingUpload) return;
    void performUpload(pendingUpload.file, pendingUpload.scope, false);
  }

  function handleCancelDuplicate() {
    setPendingUpload(null);
    setDuplicates(null);
  }

  async function handleDelete(id: string) {
    const confirmed = window.confirm("ต้องการลบเอกสารนี้ใช่หรือไม่?");
    if (!confirmed) {
      return;
    }
    setDeletingId(id);
    try {
      await api.deleteDocument(id);
      await reload(filter);
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
      await reload(filter);
    } catch (err) {
      setMoveError(err instanceof ApiClientError ? err.response.error.message : "ย้ายขอบเขตไม่สำเร็จ");
    } finally {
      setMoving(false);
    }
  }

  const canUpload = uploadScope.scopeType === "company" || Boolean(uploadScope.scopeId);
  const canConfirmMove = moveScope.scopeType === "company" || Boolean(moveScope.scopeId);

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <p className="text-xs text-muted-foreground">
          รองรับ .pptx, .pdf, .docx, .xlsx — ใช้เอกสารเดิมที่มีอยู่แล้วได้เลย ไม่ต้องทำใหม่
        </p>
        <div className="flex flex-col items-end gap-2">
          <div className="w-64">
            <CompanyOrCategoryScopeFields
              scopeType={uploadScope.scopeType as "company" | "category"}
              scopeId={uploadScope.scopeId}
              categories={categories}
              onChange={setUploadScope}
              testidPrefix="documents-upload-scope"
            />
          </div>
          <input
            ref={inputRef}
            type="file"
            accept=".pptx,.pdf,.docx,.xlsx"
            className="hidden"
            onChange={handleFileSelected}
            data-testid="documents-upload-file-input"
          />
          <Button
            data-testid="documents-upload-button"
            variant="secondary"
            onClick={() => inputRef.current?.click()}
            disabled={uploading || !canUpload}
          >
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
        <TableSkeleton columns={6} />
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
                <TableHead className="px-4">ขอบเขต</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4">อัปโหลดเมื่อ</TableHead>
                <TableHead className="px-4">จัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {documents.map((doc) => {
                const slideLesson = slideLessonFor(doc, lessons);
                return (
                  <TableRow key={doc.id} data-testid={`document-row-${doc.id}`}>
                    <TableCell className="px-4 py-3">
                      {doc.fileName}
                      {slideLesson && (
                        <Badge variant="secondary" className="ml-2">
                          ใช้เป็นสไลด์ของบทเรียน {slideLesson.title}
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="px-4 py-3 text-muted-foreground">{formatSize(doc.sizeBytes)}</TableCell>
                    <TableCell className="px-4 py-3 text-muted-foreground">
                      {scopeLabel(doc.scopeType, doc.scopeId, categories, lessons)}
                    </TableCell>
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
                          data-testid={`document-row-${doc.id}-chunks-link`}
                        >
                          ดูข้อความที่แปลงได้
                        </AdminLink>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => openMoveDialog(doc)}
                          data-testid={`document-row-${doc.id}-move-button`}
                        >
                          ย้ายขอบเขต
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(doc.id)}
                          disabled={deletingId === doc.id}
                          data-testid={`document-row-${doc.id}-delete-button`}
                        >
                          {deletingId === doc.id ? <Spinner /> : "ลบ"}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={movingDoc !== null} onOpenChange={(next) => !next && setMovingDoc(null)}>
        <DialogContent className="max-w-md" data-testid="document-move-dialog">
          <DialogHeader>
            <DialogTitle>ย้ายขอบเขต — {movingDoc?.fileName}</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <CompanyOrCategoryScopeFields
              scopeType={moveScope.scopeType as "company" | "category"}
              scopeId={moveScope.scopeId}
              categories={categories}
              onChange={setMoveScope}
              testidPrefix="document-move-scope"
            />
            {moveError && <p className="text-xs text-destructive">{moveError}</p>}
            <div className="flex justify-end gap-2">
              <Button
                variant="ghost"
                onClick={() => setMovingDoc(null)}
                disabled={moving}
                data-testid="document-move-cancel-button"
              >
                ยกเลิก
              </Button>
              <Button
                onClick={handleMoveConfirm}
                disabled={!canConfirmMove || moving}
                data-testid="document-move-confirm-button"
              >
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

      <DocumentDuplicateDialog
        open={duplicates !== null}
        duplicates={duplicates}
        scopeLabel={(scopeType, scopeId) => scopeLabel(scopeType, scopeId, categories, lessons)}
        uploading={uploading}
        onCancel={handleCancelDuplicate}
        onUploadAnyway={handleUploadAnyway}
      />
    </div>
  );
}

"use client";

import type { DuplicateDocumentDto, DuplicateDocumentsResponse } from "@/types/domain";
import { formatDateTimeTh } from "@/utils/format";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Spinner } from "@/components/ui/spinner";

type Props = {
  open: boolean;
  duplicates: DuplicateDocumentsResponse | null;
  scopeLabel: (scopeType: DuplicateDocumentDto["scopeType"], scopeId: string | undefined) => string;
  uploading: boolean;
  onCancel: () => void;
  onUploadAnyway: () => void;
};

function duplicateKind(hashMatch: boolean, nameMatch: boolean): string {
  if (hashMatch && nameMatch) return "ชื่อไฟล์และเนื้อหาซ้ำกับไฟล์ที่มีอยู่แล้ว";
  if (hashMatch) return "เนื้อหาเหมือนไฟล์ที่มีอยู่แล้วทุกตัวอักษร (ชื่อไฟล์ต่างกัน)";
  return "ชื่อไฟล์ซ้ำกับไฟล์ที่มีอยู่แล้ว (เนื้อหาต่างกัน)";
}

/**
 * KL-21/KL-22 - shown when POST /api/documents returns 409 because CS's upload matched KL-19
 * (content hash) and/or KL-20 (file name). Never blocks: "อัปโหลดต่อไป" re-sends the exact same
 * upload with checkDuplicate=false so the file always goes through if CS confirms it.
 */
export function DocumentDuplicateDialog({
  open,
  duplicates,
  scopeLabel,
  uploading,
  onCancel,
  onUploadAnyway,
}: Props) {
  if (!duplicates) return null;

  const byHashIds = new Set(duplicates.duplicateByHash.map((d) => d.id));
  const byNameIds = new Set(duplicates.duplicateByFileName.map((d) => d.id));
  const merged = new Map<string, DuplicateDocumentDto>();
  for (const item of [...duplicates.duplicateByHash, ...duplicates.duplicateByFileName]) {
    merged.set(item.id, item);
  }

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onCancel()}>
      <DialogContent className="max-w-lg" data-testid="document-duplicate-dialog">
        <DialogHeader>
          <DialogTitle>ไฟล์นี้อาจซ้ำกับไฟล์ที่มีอยู่แล้ว</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-3">
          <ul className="flex flex-col gap-2">
            {[...merged.values()].map((dup) => (
              <li key={dup.id} className="rounded-lg border p-2 text-sm" data-testid={`document-duplicate-${dup.id}`}>
                <p className="font-medium">{duplicateKind(byHashIds.has(dup.id), byNameIds.has(dup.id))}</p>
                <p className="text-muted-foreground">
                  {dup.fileName} · {scopeLabel(dup.scopeType, dup.scopeId)} · อัปโหลดเมื่อ {formatDateTimeTh(dup.createdAt)}
                </p>
              </li>
            ))}
          </ul>
          {/* KL-22/R-20 - documents uploaded before this feature shipped have ContentHash = null
              and are never flagged by KL-19; this must stay visible, not just in a design doc. */}
          <p className="text-xs text-muted-foreground">
            การเตือนนี้ครอบเฉพาะเอกสารที่อัปโหลดหลังระบบเปิดใช้การตรวจไฟล์ซ้ำเท่านั้น เอกสารที่อัปโหลดไว้ก่อนหน้านี้จะไม่ถูกจับว่าซ้ำ
          </p>
          <div className="flex justify-end gap-2">
            <Button
              variant="ghost"
              onClick={onCancel}
              disabled={uploading}
              data-testid="document-duplicate-cancel-button"
            >
              ยกเลิก
            </Button>
            <Button onClick={onUploadAnyway} disabled={uploading} data-testid="document-duplicate-upload-anyway-button">
              {uploading ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังอัปโหลด...
                </>
              ) : (
                "อัปโหลดต่อไป"
              )}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

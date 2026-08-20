"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { CategoryMovePreview } from "@/types/domain";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Spinner } from "@/components/ui/spinner";

type Props = {
  open: boolean;
  lessonId: string;
  currentCategoryId: string;
  targetCategoryId: string;
  targetCategoryName: string;
  /** Fires once PUT /api/lessons/{id}/category has committed - the caller still owns saving the
   * rest of the form. */
  onConfirmed: () => void;
  onCancel: () => void;
};

/**
 * R3.1/TX-10 - a category change never saves silently. This dialog fetches the four move-preview
 * numbers as soon as it opens, and only calls PUT /api/lessons/{id}/category after the CS
 * explicitly confirms having seen them.
 */
export function CategoryMovePreviewDialog({
  open,
  lessonId,
  currentCategoryId,
  targetCategoryId,
  targetCategoryName,
  onConfirmed,
  onCancel,
}: Props) {
  const [preview, setPreview] = useState<CategoryMovePreview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [moving, setMoving] = useState(false);

  useEffect(() => {
    if (!open) return;
    setPreview(null);
    setError(null);
    setLoading(true);
    api
      .getCategoryMovePreview(currentCategoryId, targetCategoryId)
      .then(setPreview)
      .catch((err) =>
        setError(err instanceof ApiClientError ? err.response.error.message : "โหลดตัวอย่างผลกระทบไม่สำเร็จ"),
      )
      .finally(() => setLoading(false));
  }, [open, currentCategoryId, targetCategoryId]);

  async function handleConfirm() {
    setMoving(true);
    setError(null);
    try {
      await api.moveLessonCategory(lessonId, targetCategoryId);
      onConfirmed();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "ย้ายหมวดไม่สำเร็จ");
    } finally {
      setMoving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onCancel()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>ย้ายบทเรียนไปหมวด &quot;{targetCategoryName}&quot;</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <p className="text-sm text-muted-foreground">
            ความรู้ที่ใช้ตอบคำถามในบทเรียนนี้จะเปลี่ยนทันทีที่ย้าย — โปรดตรวจสอบผลกระทบก่อนยืนยัน
          </p>

          {loading && (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Spinner />
              กำลังตรวจสอบผลกระทบ...
            </p>
          )}

          {preview && (
            <dl className="grid grid-cols-2 gap-3 rounded-lg border p-3 text-sm">
              <div>
                <dt className="text-xs text-muted-foreground">เอกสารที่จะเสีย</dt>
                <dd className="font-semibold text-destructive">{preview.losingDocuments}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">เอกสารที่จะได้</dt>
                <dd className="font-semibold text-primary">{preview.gainingDocuments}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Q&amp;A ที่จะเสีย</dt>
                <dd className="font-semibold text-destructive">{preview.losingQnAs}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Q&amp;A ที่จะได้</dt>
                <dd className="font-semibold text-primary">{preview.gainingQnAs}</dd>
              </div>
            </dl>
          )}

          {error && <p className="text-xs text-destructive">{error}</p>}

          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={onCancel} disabled={moving}>
              ยกเลิก
            </Button>
            <Button onClick={handleConfirm} disabled={!preview || moving}>
              {moving ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังย้าย...
                </>
              ) : (
                "ยืนยันย้ายหมวด"
              )}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

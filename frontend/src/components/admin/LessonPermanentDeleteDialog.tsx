"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { LessonTrashItem } from "@/types/domain";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field, FieldError, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { toast } from "@/components/ui/toast";

type Props = {
  /** null = closed. Owner-only (LT-2) - callers must not render the trigger for anyone else;
   * this dialog does not re-check the role itself, matching this module's "UI hides as a second
   * layer, the server is the real control" convention. */
  lesson: LessonTrashItem | null;
  onClose: () => void;
  /** Called once the server has accepted the request (202 - queued, not yet deleted). */
  onQueued: () => void;
};

/** LT-2/LT-10 - manual permanent delete requires typing the lesson's exact title, not a checkbox
 * or a plain confirm(). The server does the real trim + ordinal-exact compare; this dialog only
 * gates the submit button so a typo doesn't round-trip for nothing. */
export function LessonPermanentDeleteDialog({ lesson, onClose, onQueued }: Props) {
  const [confirmationTitle, setConfirmationTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setConfirmationTitle("");
    setError(null);
    setSubmitting(false);
  }, [lesson]);

  if (!lesson) {
    return null;
  }

  const canSubmit = confirmationTitle.trim().length > 0 && !submitting;

  async function handleConfirm() {
    if (!lesson || !canSubmit) return;
    setSubmitting(true);
    setError(null);
    try {
      await api.requestLessonPermanentDelete(lesson.id, { confirmationTitle });
      // 202 - queued for the durable worker, not deleted yet (LT-10/LT-14). The row still shows
      // "purging" from the next reload; this toast must not claim the lesson is already gone.
      toast.add({ title: "ตั้งคิวลบถาวรบทเรียนแล้ว", type: "success" });
      onQueued();
      onClose();
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.response.error.message : "ลบถาวรบทเรียนไม่สำเร็จ");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open onOpenChange={(nextOpen) => !nextOpen && !submitting && onClose()}>
      <DialogContent className="max-w-lg" data-testid="lesson-permanent-delete-dialog">
        <DialogHeader>
          <DialogTitle>ลบถาวรบทเรียน &ldquo;{lesson.title}&rdquo;</DialogTitle>
          <DialogDescription>
            การลบถาวรย้อนกลับไม่ได้ ระบบจะลบเอกสาร คำถาม-คำตอบ และบทพูดของบทเรียนนี้ทั้งหมด
            พิมพ์ชื่อบทเรียนให้ตรงกันเพื่อยืนยัน
          </DialogDescription>
        </DialogHeader>

        <Field data-invalid={Boolean(error)}>
          <FieldLabel htmlFor="lesson-permanent-delete-confirmation-title">
            พิมพ์ &ldquo;{lesson.title}&rdquo; เพื่อยืนยัน
          </FieldLabel>
          <Input
            id="lesson-permanent-delete-confirmation-title"
            value={confirmationTitle}
            disabled={submitting}
            aria-invalid={Boolean(error)}
            onChange={(event) => {
              setConfirmationTitle(event.target.value);
              setError(null);
            }}
            data-testid="lesson-permanent-delete-confirmation-input"
          />
          <FieldError>{error}</FieldError>
        </Field>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            disabled={submitting}
            onClick={onClose}
            data-testid="lesson-permanent-delete-cancel-button"
          >
            ยกเลิก
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={!canSubmit}
            onClick={() => void handleConfirm()}
            data-testid="lesson-permanent-delete-confirm-button"
          >
            {submitting && <Spinner data-icon="inline-start" />}
            ลบถาวร
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

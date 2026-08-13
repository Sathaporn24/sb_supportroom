"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { tutorConfig } from "@/config/tutor-config";
import { addHours } from "@/utils/format";
import type { LessonConfig, TrainingLink } from "@/types/domain";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";
import { LockIcon } from "@/components/ui/icons";
import { Spinner } from "@/components/ui/Spinner";

type Props = {
  open: boolean;
  onClose: () => void;
  lesson: LessonConfig | null;
};

function toLocalInputValue(iso: string): string {
  const date = new Date(iso);
  const offsetMs = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

export function CreateTrainingLinkModal({ open, onClose, lesson }: Props) {
  const [recipientOrgName, setRecipientOrgName] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [created, setCreated] = useState<TrainingLink | null>(null);
  const [origin, setOrigin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  useEffect(() => {
    if (open) {
      setRecipientOrgName("");
      setExpiresAt(toLocalInputValue(addHours(new Date().toISOString(), tutorConfig.defaultSessionExpiryHours)));
      setCreated(null);
      setError(null);
      setIsCreating(false);
      setOrigin(window.location.origin);
    }
  }, [open]);

  async function handleCreate() {
    if (!lesson) return;
    setError(null);
    setIsCreating(true);
    try {
      const { link } = await api.createTrainingLink({
        lessonSlug: lesson.slug,
        recipientOrgName: recipientOrgName || undefined,
        expiresAt: new Date(expiresAt).toISOString(),
      });
      setCreated(link);
    } catch (err) {
      setError(err instanceof Error ? err.message : "สร้างลิงก์การเรียนไม่สำเร็จ");
    } finally {
      setIsCreating(false);
    }
  }

  return (
    <Modal open={open} onClose={onClose} title={created ? "สร้างลิงก์การเรียนสำเร็จ" : "สร้างลิงก์การเรียน"}>
      {created ? (
        <div className="space-y-4">
          <p className="break-all rounded-lg border border-room-border bg-room-panelAlt px-3 py-2 text-sm text-room-text">
            {origin}/join/{created.token}
          </p>
          <div className="flex flex-wrap gap-2">
            <CopyLinkButton url={`${origin}/join/${created.token}`} />
            <Button variant="ghost" onClick={onClose}>
              เสร็จสิ้น
            </Button>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {lesson && (
            <div className="flex items-center gap-2 rounded-lg border border-room-accent bg-room-accentSoft/50 px-3 py-2">
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-room-accent text-room-bg">
                <LockIcon className="h-4 w-4" />
              </span>
              <p className="min-w-0 flex-1 truncate text-xs font-medium text-room-text">{lesson.title}</p>
              <Badge tone="success">
                <span className="whitespace-nowrap">พร้อมใช้งาน</span>
              </Badge>
            </div>
          )}

          {error && <p className="text-xs text-red-600">{error}</p>}

          {/* No "ชื่อผู้รับลิงก์" field any more - one link goes to a whole department and each
              person types their own name on the join screen (CORE_FEATURE_SPEC §2.1). */}
          <label className="block text-sm">
            <span className="mb-1 block text-room-muted">หน่วยงาน (ไม่บังคับ)</span>
            <input
              value={recipientOrgName}
              onChange={(e) => setRecipientOrgName(e.target.value)}
              placeholder="เช่น โรงเรียนวัดโพธิ์ / สาขาสีลม / ฝ่ายบุคคล"
              className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block text-room-muted">วันหมดอายุลิงก์</span>
            <input
              type="datetime-local"
              value={expiresAt}
              onChange={(e) => setExpiresAt(e.target.value)}
              className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
            />
          </label>
          <Button className="w-full" onClick={handleCreate} disabled={!lesson || isCreating}>
            {isCreating ? (
              <>
                <Spinner className="h-4 w-4" />
                กำลังสร้างลิงก์...
              </>
            ) : (
              "สร้างลิงก์การเรียน"
            )}
          </Button>
        </div>
      )}
    </Modal>
  );
}

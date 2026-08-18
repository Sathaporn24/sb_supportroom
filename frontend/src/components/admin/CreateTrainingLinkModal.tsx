"use client";

import { useEffect, useState } from "react";
import { LockIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { tutorConfig } from "@/config/tutor-config";
import { addHours } from "@/utils/format";
import type { LessonConfig, TrainingLink } from "@/types/domain";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";
import { CopyLinkButton } from "@/components/admin/CopyLinkButton";

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
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-h-[90vh] max-w-md overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{created ? "สร้างลิงก์การเรียนสำเร็จ" : "สร้างลิงก์การเรียน"}</DialogTitle>
        </DialogHeader>

        {created ? (
          <div className="flex flex-col gap-4">
            <p className="rounded-lg border bg-muted px-3 py-2 text-sm break-all">
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
          <div className="flex flex-col gap-4">
            {lesson && (
              <div className="flex items-center gap-2 rounded-lg border border-primary bg-primary/10 px-3 py-2">
                <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground">
                  <LockIcon className="size-4" />
                </span>
                <p className="min-w-0 flex-1 truncate text-xs font-medium">{lesson.title}</p>
                <Badge>พร้อมใช้งาน</Badge>
              </div>
            )}

            {error && <p className="text-xs text-destructive">{error}</p>}

            {/* No "ชื่อผู้รับลิงก์" field any more - one link goes to a whole department and each
                person types their own name on the join screen (CORE_FEATURE_SPEC §2.1). */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="recipient-org">หน่วยงาน (ไม่บังคับ)</Label>
              <Input
                id="recipient-org"
                value={recipientOrgName}
                onChange={(e) => setRecipientOrgName(e.target.value)}
                placeholder="เช่น โรงเรียนวัดโพธิ์ / สาขาสีลม / ฝ่ายบุคคล"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="expires-at">วันหมดอายุลิงก์</Label>
              <Input
                id="expires-at"
                type="datetime-local"
                value={expiresAt}
                onChange={(e) => setExpiresAt(e.target.value)}
              />
            </div>

            <Button className="w-full" onClick={handleCreate} disabled={!lesson || isCreating}>
              {isCreating ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังสร้างลิงก์...
                </>
              ) : (
                "สร้างลิงก์การเรียน"
              )}
            </Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

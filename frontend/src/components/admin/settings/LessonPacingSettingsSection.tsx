"use client";

import { useCallback, useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";
import { toast } from "@/components/ui/toast";
import type { CompanyLessonPacing } from "@/types/domain";
import { resolveSectionAccess } from "@/components/admin/settings/section-access";
import { LESSON_PACING_SECTION_ACCESS } from "@/components/admin/settings/lesson-pacing-access";
import {
  parseLessonPacingField,
  type LessonPacingFieldName,
} from "@/components/admin/settings/lesson-pacing-fields";

const FIELD_LABELS: Record<LessonPacingFieldName, string> = {
  introWaitMs: "ระยะรอก่อนเริ่มสอน",
  breathPauseMs: "ช่วงหยุดหายใจระหว่างสไลด์",
  finalQuestionWaitMs: "ช่วงเปิดให้ถามคำถามสุดท้าย",
};

const FIELD_HINTS: Record<LessonPacingFieldName, string> = {
  introWaitMs: "ตั้งแต่เข้าห้องจนเริ่มบรรยายสไลด์แรก (0-60000 มิลลิวินาที)",
  breathPauseMs: "หยุดพักสั้นๆ ก่อนเปลี่ยนไปสไลด์ถัดไป (0-10000 มิลลิวินาที)",
  finalQuestionWaitMs: "เปิดให้ถามคำถามสุดท้ายก่อนปิดห้องอัตโนมัติ (0-120000 มิลลิวินาที)",
};

const FIELD_ORDER: LessonPacingFieldName[] = ["introWaitMs", "breathPauseMs", "finalQuestionWaitMs"];

type FieldState = Record<LessonPacingFieldName, string>;
type FieldErrors = Partial<Record<LessonPacingFieldName, string>>;

function toFieldState(pacing: CompanyLessonPacing): FieldState {
  return {
    introWaitMs: String(pacing.introWaitMs),
    breathPauseMs: String(pacing.breathPauseMs),
    finalQuestionWaitMs: String(pacing.finalQuestionWaitMs),
  };
}

export function LessonPacingSettingsSection({ companyId }: { companyId: string }) {
  const { user } = useAdminSession();
  const role = user?.role ?? null;
  const canEdit = role != null && resolveSectionAccess(LESSON_PACING_SECTION_ACCESS, role).canEdit;

  const [fields, setFields] = useState<FieldState | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError(null);
    try {
      const pacing = await api.getCompanyLessonPacing(companyId);
      setFields(toFieldState(pacing));
    } catch (caught) {
      setLoadError(caught instanceof ApiClientError ? caught.message : "โหลดค่าจังหวะการสอนไม่สำเร็จ");
    } finally {
      setLoading(false);
    }
  }, [companyId]);

  useEffect(() => {
    void load();
  }, [load]);

  function handleChange(field: LessonPacingFieldName, value: string) {
    setFields((current) => (current ? { ...current, [field]: value } : current));
    setErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function handleSave() {
    if (!fields) return;

    const parsed: Partial<Record<LessonPacingFieldName, number>> = {};
    const nextErrors: FieldErrors = {};
    for (const field of FIELD_ORDER) {
      const result = parseLessonPacingField(field, fields[field]);
      if (result.ok) {
        parsed[field] = result.value;
      } else {
        nextErrors[field] = result.error;
      }
    }

    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    setSaving(true);
    setSaveError(null);
    try {
      const updated = await api.updateCompanyLessonPacing(companyId, {
        introWaitMs: parsed.introWaitMs!,
        breathPauseMs: parsed.breathPauseMs!,
        finalQuestionWaitMs: parsed.finalQuestionWaitMs!,
      });
      setFields(toFieldState(updated));
      toast.add({ title: "บันทึกค่าจังหวะการสอนแล้ว", type: "success" });
    } catch (caught) {
      setSaveError(caught instanceof ApiClientError ? caught.message : "บันทึกค่าจังหวะการสอนไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>จังหวะการสอน (ระดับบริษัท)</CardTitle>
        <CardDescription>
          ค่านี้มีผลกับทุกบทเรียนของบริษัทนี้ตั้งแต่การเข้าห้องเรียนครั้งถัดไปเท่านั้น ห้องที่
          กำลังเรียนอยู่ตอนนี้จะไม่เปลี่ยนกลางคัน
        </CardDescription>
      </CardHeader>

      <CardContent className="flex flex-col gap-4">
        {loading ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Spinner className="size-4" />
            กำลังโหลดค่าปัจจุบัน...
          </div>
        ) : loadError ? (
          <Alert variant="destructive" role="alert">
            <AlertDescription>{loadError}</AlertDescription>
          </Alert>
        ) : fields ? (
          <>
            {!canEdit && (
              <p className="text-sm text-muted-foreground">
                คุณมีสิทธิ์ดูค่านี้เท่านั้น ต้องเป็นเจ้าของหรือแอดมินของบริษัทนี้จึงจะแก้ไขได้
              </p>
            )}
            {saveError && (
              <Alert variant="destructive" role="alert">
                <AlertDescription>{saveError}</AlertDescription>
              </Alert>
            )}
            {FIELD_ORDER.map((field) => (
              <div key={field} className="flex flex-col gap-1.5">
                <Label htmlFor={`lesson-pacing-${field}`}>{FIELD_LABELS[field]}</Label>
                <Input
                  id={`lesson-pacing-${field}`}
                  data-testid={`lesson-pacing-${field}-input`}
                  type="number"
                  inputMode="numeric"
                  value={fields[field]}
                  disabled={!canEdit || saving}
                  onChange={(event) => handleChange(field, event.target.value)}
                  aria-invalid={Boolean(errors[field])}
                />
                <p className="text-xs text-muted-foreground">{FIELD_HINTS[field]}</p>
                {errors[field] && <p className="text-xs text-destructive">{errors[field]}</p>}
              </div>
            ))}
          </>
        ) : null}
      </CardContent>

      {canEdit && fields && !loading && !loadError && (
        <CardFooter>
          <Button data-testid="lesson-pacing-save-button" onClick={() => void handleSave()} disabled={saving}>
            {saving && <Spinner className="size-4" data-icon="inline-start" />}
            บันทึก
          </Button>
        </CardFooter>
      )}
    </Card>
  );
}

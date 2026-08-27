"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { CheckCircle2Icon, CircleIcon, XCircleIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { LessonConfigInput, PdfPreviewSessionResponse } from "@/types/domain";
import { AdminLink } from "@/components/admin/AdminLink";
import { SlideNarrationEditorCard } from "@/components/admin/SlideNarrationEditorCard";
import { getPdfFilePageNumber } from "@/lib/pdf-slide";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Progress, ProgressLabel } from "@/components/ui/progress";
import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/utils";

type ChecklistItemStatus = "pending" | "active" | "success" | "failed";

/** NR-22 - one status icon set shared by all 4 checklist rows, built only from lucide-react +
 * shadcn semantic tokens per R10.4 §⚙️ (no new icon/animation dependency). */
function ChecklistItem({ label, status, testId }: { label: string; status: ChecklistItemStatus; testId: string }) {
  return (
    <div className="flex items-center gap-2 text-sm" data-testid={testId} data-status={status}>
      {status === "success" && <CheckCircle2Icon className="size-4 shrink-0 text-primary" />}
      {status === "failed" && <XCircleIcon className="size-4 shrink-0 text-destructive" />}
      {status === "active" && <Spinner className="size-4 shrink-0 text-primary" />}
      {status === "pending" && <CircleIcon className="size-4 shrink-0 text-muted-foreground" />}
      <span className={cn(status === "pending" ? "text-muted-foreground" : "text-foreground")}>{label}</span>
    </div>
  );
}

type FormSnapshot = {
  slug: string;
  categoryId: string;
  title: string;
  description?: string;
  isActive: boolean;
};

type StepError = { step: 1 | 2 | 3; message: string };

type Props = {
  previewSession: PdfPreviewSessionResponse;
  file: File;
  formSnapshot: FormSnapshot;
  onBack: () => void;
};

function extractErrorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiClientError ? err.response.error.message : fallback;
}

/**
 * R4.6/Module J (NR-10..NR-16) - the content-management phase that opens after "สร้างบทเรียน" for
 * a create-mode PDF lesson, before anything is persisted (R4.6.1/R4.6.5/R4.6.6). Everything here
 * is client-side state until "ยืนยันสร้างบทเรียน" runs the fixed 4-step commit of NR-12.
 */
export function PdfLessonContentPhase({ previewSession: initialSession, file: initialFile, formSnapshot, onBack }: Props) {
  const router = useRouter();
  const replaceInputRef = useRef<HTMLInputElement>(null);

  const [previewSession, setPreviewSession] = useState(initialSession);
  const [file, setFile] = useState(initialFile);
  const [replacing, setReplacing] = useState(false);
  const [replaceError, setReplaceError] = useState<string | null>(null);

  const [draft, setDraft] = useState<Record<string, string>>(() =>
    Object.fromEntries(initialSession.slides.map((s) => [s.slideObjectId, s.narrationText])),
  );
  const [touchedIds, setTouchedIds] = useState<Set<string>>(new Set());
  // EX-9 - client-only draft until the confirm step commits it in POST /api/lessons's
  // excludedSlideObjectIds (NR-12 step 3). Never call the EX-4 toggle endpoint here - there is no
  // LessonId yet.
  const [excludedSlideObjectIds, setExcludedSlideObjectIds] = useState<Set<string>>(new Set());
  const [warningDialogOpen, setWarningDialogOpen] = useState(false);

  // NR-20 - the commit modal's own state, set true only at the start of commit() and never
  // computed from commitStarted/lessonId/committing (a step-1 failure never sets lessonId, so
  // deriving `open` from it would leave the modal never opening at all). The only code path that
  // sets this back to false is the "กลับไปแก้ข้อมูลบทเรียน" button NR-24 allows for a step-1 failure.
  const [commitModalOpen, setCommitModalOpen] = useState(false);

  const [committing, setCommitting] = useState(false);
  const [stepError, setStepError] = useState<StepError | null>(null);
  const [lessonId, setLessonId] = useState<string | null>(null);
  const [lessonSlug, setLessonSlug] = useState<string | null>(null);
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [documentLinked, setDocumentLinked] = useState(false);
  const [narrationResults, setNarrationResults] = useState<Record<string, "success" | "failed">>({});

  // Refs mirror the state above so the async commit() function always reads the latest progress
  // synchronously (React state updates aren't visible until the next render) - this is what lets
  // a retry resume from the right step per NR-13 instead of redoing work that already succeeded.
  const lessonIdRef = useRef<string | null>(null);
  const lessonSlugRef = useRef<string | null>(null);
  const documentIdRef = useRef<string | null>(null);
  const documentLinkedRef = useRef(false);

  const commitStarted = lessonId !== null;

  function buildLessonInput(
    pdfDocumentResourceId: string | undefined,
    excludedSlideObjectIdsForSave?: string[],
  ): LessonConfigInput {
    return {
      slug: formSnapshot.slug,
      categoryId: formSnapshot.categoryId,
      title: formSnapshot.title,
      description: formSnapshot.description,
      slidesSourceUrl: "",
      slidesEmbedUrl: null,
      contentSourceType: "pdf",
      pdfDocumentResourceId,
      slideConfigs: previewSession.slides.map((s) => ({
        slideObjectId: s.slideObjectId,
        slideIndex: s.index,
        videoDurationMs: null,
      })),
      isActive: formSnapshot.isActive,
      excludedSlideObjectIds: excludedSlideObjectIdsForSave,
    };
  }

  // NR-21(ก) - navigation out of this phase is no longer a side effect of a successful flush, in
  // either of this function's two call sites (commit()'s success path and
  // handleRetryFailedNarrations()). The only thing that changes pages now is the CS clicking the
  // confirm button the commit modal shows in its `succeeded` state (NR-24).
  async function flushNarrations(lessonIdValue: string, ids: string[]) {
    const results: Record<string, "success" | "failed"> = {};
    for (const id of ids) {
      try {
        await api.saveLessonNarration(lessonIdValue, id, draft[id] ?? "");
        results[id] = "success";
      } catch {
        results[id] = "failed";
      }
    }
    setNarrationResults((current) => ({ ...current, ...results }));
  }

  // P11-02 fix - step 3 already commits excludedSlideObjectIds; a page edited and then excluded
  // must not also be flushed as a narration write in step 4, or EX-12(ก) correctly rejects it and
  // the create flow reports a permanent failed page for a page that was never meant to be taught.
  // One derived set feeds step 4's flushNarrations call, the progress totals, and retry state so
  // they can never disagree with each other.
  const touchedAndNotExcludedIds = new Set(
    Array.from(touchedIds).filter((id) => !excludedSlideObjectIds.has(id)),
  );

  /** NR-12 - the fixed 4-step commit, in order, never 3↔4 swapped. Resumable: each step is
   * skipped if its ref already holds a result, which is what makes NR-13's per-step retries work
   * without redoing (or re-uploading) anything that already succeeded. */
  async function commit() {
    setCommitModalOpen(true);
    setCommitting(true);
    setStepError(null);

    if (!lessonIdRef.current) {
      try {
        const { lesson: created } = await api.saveLesson(buildLessonInput(undefined));
        lessonIdRef.current = created.id;
        lessonSlugRef.current = created.slug;
        setLessonId(created.id);
        setLessonSlug(created.slug);
      } catch (err) {
        setStepError({ step: 1, message: extractErrorMessage(err, "สร้างบทเรียนไม่สำเร็จ") });
        setCommitting(false);
        return;
      }
    }

    if (!documentIdRef.current) {
      try {
        const { document } = await api.uploadDocument(
          file,
          { scopeType: "lesson", scopeId: lessonIdRef.current },
          false,
        );
        documentIdRef.current = document.id;
        setDocumentId(document.id);
      } catch (err) {
        setStepError({ step: 2, message: extractErrorMessage(err, "อัปโหลดไฟล์ PDF ไม่สำเร็จ") });
        setCommitting(false);
        return;
      }
    }

    if (!documentLinkedRef.current) {
      try {
        await api.saveLesson(buildLessonInput(documentIdRef.current, Array.from(excludedSlideObjectIds)));
        documentLinkedRef.current = true;
        setDocumentLinked(true);
      } catch (err) {
        setStepError({ step: 3, message: extractErrorMessage(err, "ผูกไฟล์กับบทเรียนไม่สำเร็จ") });
        setCommitting(false);
        return;
      }
    }

    await flushNarrations(lessonIdRef.current, Array.from(touchedAndNotExcludedIds));
    setCommitting(false);
  }

  async function handleRetryFailedNarrations() {
    if (!lessonIdRef.current) return;
    const failedIds = Object.entries(narrationResults)
      .filter(([, v]) => v === "failed")
      .map(([id]) => id);
    if (failedIds.length === 0) return;
    setCommitting(true);
    await flushNarrations(lessonIdRef.current, failedIds);
    setCommitting(false);
  }

  // NR-15 - an excluded page never counts toward either warning: it won't be taught, so an
  // unreviewed or blank narration on it means nothing. Cutting every blank page can legitimately
  // clear the warning entirely.
  const remainingSlides = previewSession.slides.filter((s) => !excludedSlideObjectIds.has(s.slideObjectId));
  const totalPages = remainingSlides.length;
  const emptyPages = remainingSlides.filter((s) => (draft[s.slideObjectId] ?? "").trim().length === 0);
  const noPageTouched = remainingSlides.every((s) => !touchedIds.has(s.slideObjectId));
  const needsConfirmWarning = noPageTouched || emptyPages.length > 0;

  function handleToggleExcluded(slideObjectId: string) {
    setExcludedSlideObjectIds((prev) => {
      const next = new Set(prev);
      if (next.has(slideObjectId)) {
        next.delete(slideObjectId);
      } else {
        next.add(slideObjectId);
      }
      return next;
    });
  }

  function handleConfirmClick() {
    if (needsConfirmWarning) {
      setWarningDialogOpen(true);
      return;
    }
    void commit();
  }

  /** NR-16 - silent replace: no dialog, no page count, because nothing has been persisted yet
   * (unlike NR-3's replace-on-an-existing-lesson case). Still under NR-4: no attempt to match old
   * pages to new ones - drafts are simply cleared. */
  async function handleReplaceFile(newFile: File) {
    setReplacing(true);
    setReplaceError(null);
    try {
      const session = await api.createPdfPreviewSession(newFile);
      setPreviewSession(session);
      setFile(newFile);
      setDraft(Object.fromEntries(session.slides.map((s) => [s.slideObjectId, s.narrationText])));
      setTouchedIds(new Set());
      setExcludedSlideObjectIds(new Set());
    } catch (err) {
      setReplaceError(extractErrorMessage(err, "อ่านไฟล์ PDF ไม่สำเร็จ"));
    } finally {
      setReplacing(false);
    }
  }

  const failedNarrationIds = Object.entries(narrationResults)
    .filter(([, v]) => v === "failed")
    .map(([id]) => id);
  const successNarrationCount = Object.values(narrationResults).filter((v) => v === "success").length;
  const totalSteps = 3 + touchedAndNotExcludedIds.size;
  const completedSteps =
    (lessonId ? 1 : 0) + (documentId ? 1 : 0) + (documentLinked ? 1 : 0) + successNarrationCount;

  // NR-20 - stepError only ever gets set for steps 1-3 (commit() returns before step 4 runs), so
  // a leftover failed narration only ever shows up once stepError is null - the two never compete
  // for which "failed step" is current.
  const failedStep: 1 | 2 | 3 | 4 | null =
    stepError?.step ?? (failedNarrationIds.length > 0 ? 4 : null);
  const modalStatus: "running" | "succeeded" | "failed" = committing
    ? "running"
    : failedStep !== null
      ? "failed"
      : "succeeded";

  const narrationTotal = touchedAndNotExcludedIds.size;
  const step1Status: ChecklistItemStatus = lessonId
    ? "success"
    : stepError?.step === 1
      ? "failed"
      : committing
        ? "active"
        : "pending";
  const step2Status: ChecklistItemStatus = documentId
    ? "success"
    : stepError?.step === 2
      ? "failed"
      : Boolean(lessonId) && committing
        ? "active"
        : "pending";
  const step3Status: ChecklistItemStatus = documentLinked
    ? "success"
    : stepError?.step === 3
      ? "failed"
      : Boolean(documentId) && committing
        ? "active"
        : "pending";
  const step4Status: ChecklistItemStatus = failedNarrationIds.length > 0
    ? "failed"
    : documentLinked && successNarrationCount === narrationTotal
      ? "success"
      : documentLinked && committing
        ? "active"
        : "pending";

  function handleBackToEditingFromFailedCreate() {
    setCommitModalOpen(false);
  }

  function handleConfirmSuccess() {
    if (!lessonSlugRef.current) return;
    router.push("/admin/lessons");
  }

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div>
        {!commitStarted && (
          <button
            type="button"
            onClick={onBack}
            className="text-xs text-muted-foreground hover:text-foreground"
            data-testid="pdf-content-phase-back-button"
          >
            ← กลับไปแก้ข้อมูลบทเรียน
          </button>
        )}
        <h1 className="mt-1 text-xl font-semibold text-primary">จัดการเนื้อหาก่อนสร้างบทเรียน: {formSnapshot.title}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          ตรวจ/แก้บทพูดของแต่ละหน้าได้ที่นี่ก่อนสร้างจริง — ยังไม่มีอะไรถูกบันทึกลงระบบจนกว่าจะกด &quot;ยืนยันสร้างบทเรียน&quot;
        </p>
      </div>

      {previewSession.isLikelyScanned && (
        <Alert variant="destructive">
          <AlertTitle>ไฟล์นี้น่าจะเป็น PDF สแกน</AlertTitle>
          <AlertDescription>
            ทุกหน้าไม่มีข้อความให้ดึงเลย — AI จะไม่มีอะไรพูดถ้าไม่พิมพ์บทพูดเองทุกหน้าในนี้
          </AlertDescription>
        </Alert>
      )}

      {!commitStarted && (
        <div className="flex flex-col gap-2">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            className="w-fit"
            disabled={replacing}
            onClick={() => replaceInputRef.current?.click()}
            data-testid="pdf-content-phase-replace-button"
          >
            {replacing ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังอ่านไฟล์ใหม่...
              </>
            ) : (
              "เปลี่ยนไฟล์ PDF"
            )}
          </Button>
          <Input
            ref={replaceInputRef}
            type="file"
            accept="application/pdf"
            className="hidden"
            onChange={(e) => {
              const f = e.target.files?.[0];
              if (f) void handleReplaceFile(f);
              e.target.value = "";
            }}
            data-testid="pdf-content-phase-replace-input"
          />
          <p className="text-xs text-muted-foreground">ไฟล์ปัจจุบัน: {file.name} ({previewSession.pageCount} หน้า)</p>
          {replaceError && <p className="text-xs text-destructive">{replaceError}</p>}
        </div>
      )}

      {!commitStarted ? (
        <div className="flex flex-col gap-4">
          {(() => {
            let nextLessonIndex = 0;
            return previewSession.slides.map((slide) => {
              const isExcluded = excludedSlideObjectIds.has(slide.slideObjectId);
              const filePageNumber = getPdfFilePageNumber(slide.slideObjectId);
              const pageLabel = isExcluded
                ? `หน้าที่ ${filePageNumber} ของไฟล์`
                : `หน้า ${nextLessonIndex + 1}`;
              if (!isExcluded) nextLessonIndex += 1;
              return (
                <SlideNarrationEditorCard
                  key={slide.slideObjectId}
                  pageLabel={pageLabel}
                  imageSrc={api.getPdfPreviewPageUrl(previewSession.previewId, filePageNumber)}
                  value={draft[slide.slideObjectId] ?? ""}
                  onChange={(text) => {
                    setDraft((prev) => ({ ...prev, [slide.slideObjectId]: text }));
                    setTouchedIds((prev) => new Set(prev).add(slide.slideObjectId));
                  }}
                  isExcluded={isExcluded}
                  onToggleExcluded={() => handleToggleExcluded(slide.slideObjectId)}
                  excludeToggleDisabled={previewSession.slides.length - excludedSlideObjectIds.size <= 1}
                  testIdPrefix={`pdf-content-phase-slide-${slide.slideObjectId}`}
                />
              );
            });
          })()}
        </div>
      ) : null}

      {!commitStarted && (
        <div className="flex justify-end">
          <Button onClick={handleConfirmClick} disabled={committing} data-testid="pdf-content-phase-confirm-button">
            {committing ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังสร้าง...
              </>
            ) : (
              "ยืนยันสร้างบทเรียน"
            )}
          </Button>
        </div>
      )}

      {/* NR-20..NR-24 - the commit modal. `open` is driven only by commitModalOpen (NR-20), never
          by commitStarted/lessonId/committing. onOpenChange never lets Esc/outside-click close it
          (NR-23(ก)) - every exit is one of the buttons NR-24 lists below, rendered per modalStatus. */}
      <Dialog open={commitModalOpen} onOpenChange={() => {}}>
        <DialogContent showCloseButton={false} className="max-w-lg" data-testid="pdf-content-phase-commit-dialog">
          <DialogHeader>
            <DialogTitle>
              {modalStatus === "running"
                ? "กำลังสร้างบทเรียน"
                : modalStatus === "succeeded"
                  ? "สร้างบทเรียนสำเร็จ"
                  : "ดำเนินการไม่สำเร็จบางขั้นตอน"}
            </DialogTitle>
            <DialogDescription>
              {modalStatus === "running" && "กรุณารอจนกว่าจะเสร็จ ห้ามปิดหน้าต่างนี้"}
              {modalStatus === "succeeded" && "บทเรียนพร้อมใช้งานแล้ว"}
              {modalStatus === "failed" && "ตรวจสอบรายการด้านล่างและลองอีกครั้งได้เลย"}
            </DialogDescription>
          </DialogHeader>

          <Progress value={(completedSteps / totalSteps) * 100} data-testid="pdf-content-phase-progress">
            <div className="flex w-full items-center justify-between">
              <ProgressLabel>ความคืบหน้า</ProgressLabel>
              <span className="text-sm text-muted-foreground tabular-nums">
                {completedSteps}/{totalSteps}
              </span>
            </div>
          </Progress>

          <div className="flex flex-col gap-2 rounded-md border p-3">
            <ChecklistItem label="สร้างบทเรียน" status={step1Status} testId="pdf-content-phase-checklist-step-1" />
            <ChecklistItem label="อัปโหลดไฟล์ PDF" status={step2Status} testId="pdf-content-phase-checklist-step-2" />
            <ChecklistItem label="ผูกไฟล์กับบทเรียน" status={step3Status} testId="pdf-content-phase-checklist-step-3" />
            <ChecklistItem
              label={`บันทึกบทพูด (สำเร็จ ${successNarrationCount}/${narrationTotal} หน้า)`}
              status={step4Status}
              testId="pdf-content-phase-checklist-step-4"
            />
          </div>

          {stepError && (
            <p className="text-sm text-destructive" data-testid="pdf-content-phase-step-error-message">
              {stepError.step === 1 && `สร้างบทเรียนไม่สำเร็จ: ${stepError.message}`}
              {stepError.step === 2 && `บทเรียนถูกสร้างแล้ว แต่อัปไฟล์ไม่สำเร็จ: ${stepError.message}`}
              {stepError.step === 3 && `อัปโหลดไฟล์สำเร็จแล้ว แต่ผูกไฟล์กับบทเรียนไม่สำเร็จ: ${stepError.message}`}
            </p>
          )}

          {!stepError && failedNarrationIds.length > 0 && (
            <p className="text-sm text-destructive" data-testid="pdf-content-phase-narration-error-message">
              บันทึกบทพูดสำเร็จ {successNarrationCount}/{narrationTotal} หน้า — เหลือ {failedNarrationIds.length} หน้าที่ยังไม่สำเร็จ
            </p>
          )}

          {/* NR-24 - the button table is fixed per status: `running` shows nothing at all,
              `succeeded` shows exactly one confirm button, `failed` shows the per-step retry plus
              an exit link (and, only for a step-1 failure, a way back into this same phase). */}
          {modalStatus !== "running" && (
            <DialogFooter>
              {modalStatus === "succeeded" && (
                <Button onClick={handleConfirmSuccess} data-testid="pdf-content-phase-commit-confirm-button">
                  ไปที่หน้ารายการบทเรียน
                </Button>
              )}

              {modalStatus === "failed" && failedStep === 1 && (
                <>
                  <Button
                    variant="outline"
                    onClick={handleBackToEditingFromFailedCreate}
                    data-testid="pdf-content-phase-commit-back-button"
                  >
                    กลับไปแก้ข้อมูลบทเรียน
                  </Button>
                  <Button
                    disabled={committing}
                    onClick={() => void commit()}
                    data-testid="pdf-content-phase-retry-button"
                  >
                    {committing ? <Spinner /> : "ลองอีกครั้ง"}
                  </Button>
                </>
              )}

              {modalStatus === "failed" && failedStep === 2 && lessonSlug && (
                <>
                  <AdminLink
                    href={`/admin/lessons/${encodeURIComponent(lessonSlug)}/narrations`}
                    className={buttonVariants({ variant: "outline" })}
                    data-testid="pdf-content-phase-narrations-link"
                  >
                    ไปหน้าแก้บทพูดต่อหน้า →
                  </AdminLink>
                  <Button
                    disabled={committing}
                    onClick={() => void commit()}
                    data-testid="pdf-content-phase-retry-button"
                  >
                    {committing ? <Spinner /> : "ลองอัปโหลดไฟล์อีกครั้ง"}
                  </Button>
                </>
              )}

              {modalStatus === "failed" && failedStep === 3 && lessonSlug && (
                <>
                  <AdminLink
                    href={`/admin/lessons/${encodeURIComponent(lessonSlug)}/narrations`}
                    className={buttonVariants({ variant: "outline" })}
                    data-testid="pdf-content-phase-narrations-link"
                  >
                    ไปหน้าแก้บทพูดต่อหน้า →
                  </AdminLink>
                  <Button
                    disabled={committing}
                    onClick={() => void commit()}
                    data-testid="pdf-content-phase-retry-button"
                  >
                    {committing ? <Spinner /> : "ลองอีกครั้ง"}
                  </Button>
                </>
              )}

              {modalStatus === "failed" && failedStep === 4 && lessonSlug && (
                <>
                  <AdminLink
                    href={`/admin/lessons/${encodeURIComponent(lessonSlug)}/narrations`}
                    className={buttonVariants({ variant: "outline" })}
                    data-testid="pdf-content-phase-narrations-link"
                  >
                    ไปหน้าแก้บทพูดต่อหน้า →
                  </AdminLink>
                  <Button
                    disabled={committing}
                    onClick={() => void handleRetryFailedNarrations()}
                    data-testid="pdf-content-phase-retry-narrations-button"
                  >
                    {committing ? <Spinner /> : "ลองใหม่เฉพาะหน้าที่เหลือ"}
                  </Button>
                </>
              )}
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>

      <AlertDialog open={warningDialogOpen} onOpenChange={setWarningDialogOpen}>
        <AlertDialogContent data-testid="pdf-content-phase-warning-dialog">
          <AlertDialogHeader>
            <AlertDialogTitle>ยังไม่ได้ตรวจบทพูดครบ</AlertDialogTitle>
            <AlertDialogDescription>
              <span className="flex flex-col gap-1">
                {noPageTouched && <span>ยังไม่ได้ตรวจบทพูดเลยทั้งหมด {totalPages} หน้า</span>}
                {emptyPages.length > 0 && <span>มี {emptyPages.length} หน้าที่บทพูดว่างเปล่า</span>}
                <span>ยืนยันสร้างต่อได้ตามปกติ — ระบบจะใช้ข้อความที่ดึงได้จากไฟล์แทนหน้าที่ยังไม่ได้แก้</span>
              </span>
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel data-testid="pdf-content-phase-warning-cancel-button">กลับไปแก้ต่อ</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                setWarningDialogOpen(false);
                void commit();
              }}
              data-testid="pdf-content-phase-warning-confirm-button"
            >
              ยืนยันสร้างต่อ
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}

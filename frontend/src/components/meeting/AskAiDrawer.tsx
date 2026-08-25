"use client";

import { useEffect, useRef, useState } from "react";
import { XIcon } from "lucide-react";
import type { SessionQuestion } from "@/types/domain";
import { answerStatusLabels } from "@/utils/session-status";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

// DtoLimits.QuestionTextMaxLength (backend) - keep this equal to the server's bound so a question
// never gets blocked by the input, sent, cleared optimistically (TQ-15), and then rejected by a
// 400 with no way to bring the typed text back (QA-03).
const QUESTION_TEXT_MAX_LENGTH = 2000;

type Props = {
  open: boolean;
  onClose: () => void;
  questions: SessionQuestion[];
  onSubmitQuestion: (text: string) => void;
  /** TQ-20 - whether the input field itself accepts typing right now. Comes from the room page's
   * tutor-state matrix; this component has no idea what runtime.state means and must not guess. */
  inputEnabled: boolean;
  /** TQ-20 - whether the Send button may be pressed right now (independent of inputEnabled: a
   * draft can sit typed-but-unsendable while a previous question is still being answered). */
  sendEnabled: boolean;
  /** Shown in the input's placeholder while inputEnabled is false, so the field explains itself
   * instead of just looking broken (TQ-18/TQ-20). */
  disabledHint?: string;
  /** QA-03 - text of the most recent typed question that failed to send (network/upstream error).
   * null once there is nothing to restore, or after this component has already consumed it. */
  failedQuestionText: string | null;
  /** Tells the tutor runtime failedQuestionText has been copied back into the draft, so it isn't
   * offered again on the next unrelated failure. */
  onFailedQuestionTextConsumed: () => void;
};

/**
 * CX-6 - the single Ask-AI drawer for this room: no CS chat channel exists in this system, so
 * there is exactly one kind of timeline entry (SessionQuestion), voice or typed, told apart only
 * by source (CS-facing, not shown here).
 */
export function AskAiDrawer({
  open,
  onClose,
  questions,
  onSubmitQuestion,
  inputEnabled,
  sendEnabled,
  disabledHint,
  failedQuestionText,
  onFailedQuestionTextConsumed,
}: Props) {
  const [draft, setDraft] = useState("");
  const listRef = useRef<HTMLDivElement | null>(null);

  const sorted = [...questions].sort((a, b) => a.createdAt.localeCompare(b.createdAt));

  useEffect(() => {
    if (!open) return;
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight });
  }, [open, sorted.length]);

  // QA-03 - a typed question that failed to send (network/upstream error) restores its text here
  // exactly once, then tells the runtime to forget it so a later, unrelated failure doesn't
  // re-offer stale text. Length-limit rejections never reach this path at all - handleSend blocks
  // those before onSubmitQuestion is ever called, so there's nothing here to recover from them.
  //
  // QA-03 residual - the learner may already be typing the *next* question (Q2) while Q1 is still
  // "processing-question" and hasn't failed yet. If Q1 then fails, restoring blindly would clobber
  // Q2 with no way to get it back. setDraft's functional form reads the draft at the moment this
  // effect fires (not a stale closure), so the restore only happens when the field is still empty -
  // otherwise the learner's in-progress Q2 wins and Q1's text is dropped, but failedQuestionText is
  // still consumed either way so it never re-offers itself once the field happens to go empty later.
  useEffect(() => {
    if (failedQuestionText === null) return;
    setDraft((current) => (current.length === 0 ? failedQuestionText : current));
    onFailedQuestionTextConsumed();
  }, [failedQuestionText, onFailedQuestionTextConsumed]);

  if (!open) {
    return null;
  }

  const trimmed = draft.trim();
  const isTooLong = trimmed.length > QUESTION_TEXT_MAX_LENGTH;
  const canSend = sendEnabled && trimmed.length > 0 && !isTooLong;

  function handleSend() {
    if (!canSend) return;
    // TQ-15 - fire-and-forget: SUBMIT_TEXT_QUESTION is dispatched synchronously by the caller,
    // and the drawer has no result to wait on here - QUESTION_ANSWERED/QUESTION_FAILED land on
    // the tutor runtime, not on a promise this component holds. Clearing immediately is safe
    // because sendEnabled turns false the instant processing starts, so there's no window to
    // double-submit the same draft. A transport/upstream failure still recovers the text via
    // failedQuestionText above; a too-long draft never gets here at all (canSend already blocks
    // it), so there is nothing left to lose here.
    onSubmitQuestion(trimmed);
    setDraft("");
  }

  return (
    <div
      data-testid="ask-ai-drawer"
      className={cn(
        "fixed inset-0 z-30 flex h-[100dvh] flex-col border bg-card shadow-2xl",
        "lg:absolute lg:inset-auto lg:right-4 lg:bottom-20 lg:h-[420px] lg:w-80 lg:rounded-xl",
      )}
    >
      <div className="flex shrink-0 items-center justify-between border-b px-4 py-3">
        <p className="text-sm font-semibold text-foreground">ถาม-ตอบกับผู้ช่วย AI</p>
        <Button
          variant="ghost"
          size="icon-lg"
          className="size-11"
          onClick={onClose}
          aria-label="ปิดหน้าต่างถาม-ตอบ"
          title="ปิดหน้าต่างถาม-ตอบ"
          data-testid="ask-ai-drawer-close-button"
        >
          <XIcon />
        </Button>
      </div>

      <div ref={listRef} className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 py-3">
        {sorted.length === 0 && (
          <p className="text-sm text-muted-foreground">กดค้างปุ่มไมค์เพื่อถามคำถาม หรือพิมพ์ข้อความได้เลยค่ะ</p>
        )}
        {sorted.map((question, index) => (
          <div key={question.id || index} className="flex flex-col gap-1">
            {question.transcript && (
              <p className="rounded-lg bg-muted px-3 py-2 text-sm text-foreground">
                {question.transcript}
                <span className="ml-2 text-xs text-muted-foreground">
                  ({answerStatusLabels[question.answerStatus]})
                </span>
              </p>
            )}
            {question.answer && (
              <p className="rounded-lg bg-primary/10 px-3 py-2 text-sm text-foreground">{question.answer}</p>
            )}
          </div>
        ))}
      </div>

      {/* RS-8 - sticky within this flex column (itself pinned to 100dvh on compact), font-size
          text-base (>=16px) so iOS Safari doesn't auto-zoom on focus, and the Send button is
          always visible/tappable rather than relying on the on-screen keyboard's Enter key. */}
      <div className="sticky bottom-0 flex shrink-0 flex-col gap-1 border-t bg-card px-3 py-2 pb-[calc(0.5rem+env(safe-area-inset-bottom))]">
        {isTooLong && (
          // QA-03 - blocks the send before it ever leaves the browser: without this, a draft
          // over the backend's limit got cleared optimistically (TQ-15) and then rejected by a
          // 400 with no way to bring the typed text back.
          <p className="text-xs text-destructive">
            ข้อความยาวเกิน {QUESTION_TEXT_MAX_LENGTH.toLocaleString("th-TH")} ตัวอักษร
            ({trimmed.length.toLocaleString("th-TH")}) กรุณาย่อข้อความก่อนส่ง
          </p>
        )}
        <div className="flex items-center gap-2">
          <Input
            data-testid="ask-ai-drawer-input"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onFocus={() => listRef.current?.scrollTo({ top: listRef.current.scrollHeight })}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                handleSend();
              }
            }}
            disabled={!inputEnabled}
            placeholder={inputEnabled ? "พิมพ์คำถาม..." : disabledHint}
            aria-invalid={isTooLong}
            // RS-8 - md:text-base overrides the shadcn Input default's `md:text-sm`: compact
            // (this project's breakpoint) runs through 1024px, which is past md (768px), so
            // without this override the field would drop below 16px exactly in that range and
            // iOS Safari would auto-zoom on focus.
            className="h-11 flex-1 text-base md:text-base"
          />
          <Button
            onClick={handleSend}
            disabled={!canSend}
            className="h-11"
            data-testid="ask-ai-drawer-submit-button"
          >
            ส่ง
          </Button>
        </div>
      </div>
    </div>
  );
}

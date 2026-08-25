"use client";

import { MicIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export type PushToTalkStatus = "idle" | "recording" | "processing" | "answering" | "disabled";

type Props = {
  status: PushToTalkStatus;
  onStart: () => void;
  onEnd: () => void;
};

const labels: Record<PushToTalkStatus, string> = {
  idle: "กดค้างเพื่อพูด",
  recording: "กำลังฟัง...",
  processing: "กำลังประมวลผล...",
  answering: "กำลังตอบ...",
  disabled: "กดค้างเพื่อพูด",
};

/**
 * RS-5 - rewritten on Pointer Events after confirming the previous mouse/touch handlers never
 * actually worked on a touchscreen: React binds touchstart/touchmove/wheel as passive listeners
 * at the root container since v17, so `e.preventDefault()` inside a React onTouchStart handler is
 * silently ignored by the browser. Pointer Events + `touch-none` (CSS `touch-action: none`) is
 * the mechanism that actually prevents page scroll and the iOS text-selection callout - CSS, not
 * a JS preventDefault, is the only thing that works against a passive listener.
 */
export function PushToTalkButton({ status, onStart, onEnd }: Props) {
  const isBusy = status === "processing" || status === "answering" || status === "disabled";
  const isRecording = status === "recording";

  function press() {
    if (isBusy || isRecording) return;
    onStart();
  }

  function release() {
    if (!isRecording) return;
    onEnd();
  }

  // QA-05 - pointercancel means the browser yanked this gesture away mid-press (a notification
  // swipe, an app switch, the OS taking over) rather than the finger lifting normally. That is
  // exactly the moment local state and reality can disagree, so recording must stop unconditionally
  // here - unlike release()'s isRecording guard, which exists only to make pointerup/keyup safe to
  // fire from a state that never started recording in the first place.
  function forceRelease() {
    onEnd();
  }

  return (
    <Button
      type="button"
      size="lg"
      aria-pressed={isRecording}
      aria-label={labels[status]}
      data-testid="room-push-to-talk-button"
      data-state={status}
      disabled={isBusy}
      onPointerDown={(e) => {
        // Captures the pointer to this button so a finger that drags off it while held down
        // still delivers pointerup here instead of wherever it lifted - without this, dragging
        // off the button mid-press leaves the recording running forever.
        e.currentTarget.setPointerCapture(e.pointerId);
        press();
      }}
      onPointerUp={release}
      onPointerCancel={forceRelease}
      onContextMenu={(e) => e.preventDefault()}
      onKeyDown={(e) => {
        if ((e.key === " " || e.key === "Enter") && !e.repeat) press();
      }}
      onKeyUp={(e) => {
        if (e.key === " " || e.key === "Enter") release();
      }}
      // Recording needs a filled "live" red that the subtle destructive variant doesn't give,
      // so this one state overrides the variant colors on purpose. touch-none/select-none/
      // touch-callout-none are the RS-5 anti-scroll/anti-selection/anti-callout trio - all CSS,
      // because preventDefault() on a passive touch listener has no effect (see comment above).
      className={cn(
        "h-11 touch-none select-none rounded-full px-5 text-sm font-semibold shadow-lg [-webkit-touch-callout:none]",
        isRecording && "animate-pulse bg-destructive text-white hover:bg-destructive",
      )}
    >
      <MicIcon data-icon="inline-start" />
      {labels[status]}
    </Button>
  );
}

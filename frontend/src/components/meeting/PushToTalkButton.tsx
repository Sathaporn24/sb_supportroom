"use client";

import type { KeyboardEvent, MouseEvent, TouchEvent } from "react";
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

export function PushToTalkButton({ status, onStart, onEnd }: Props) {
  const isBusy = status === "processing" || status === "answering" || status === "disabled";
  const isRecording = status === "recording";

  function press(e: MouseEvent | TouchEvent | KeyboardEvent) {
    e.preventDefault();
    if (isBusy || isRecording) return;
    onStart();
  }

  function release(e: MouseEvent | TouchEvent | KeyboardEvent) {
    e.preventDefault();
    if (!isRecording) return;
    onEnd();
  }

  return (
    <Button
      type="button"
      size="lg"
      aria-pressed={isRecording}
      aria-label={labels[status]}
      disabled={isBusy}
      onMouseDown={press}
      onMouseUp={release}
      onMouseLeave={release}
      onTouchStart={press}
      onTouchEnd={release}
      onKeyDown={(e) => {
        if ((e.key === " " || e.key === "Enter") && !e.repeat) press(e);
      }}
      onKeyUp={(e) => {
        if (e.key === " " || e.key === "Enter") release(e);
      }}
      // Recording needs a filled "live" red that the subtle destructive variant doesn't give,
      // so this one state overrides the variant colors on purpose.
      className={cn(
        "h-11 rounded-full px-5 text-sm font-semibold shadow-lg",
        isRecording && "animate-pulse bg-destructive text-white hover:bg-destructive",
      )}
    >
      <MicIcon data-icon="inline-start" />
      {labels[status]}
    </Button>
  );
}

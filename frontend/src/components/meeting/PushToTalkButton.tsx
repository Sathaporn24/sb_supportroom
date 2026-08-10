"use client";

import type { KeyboardEvent, MouseEvent, TouchEvent } from "react";
import { MicIcon } from "@/components/ui/icons";

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
    <button
      type="button"
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
      className={`flex select-none items-center gap-2 rounded-full px-5 py-3 text-sm font-semibold shadow-lg transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-room-accent ${
        isRecording
          ? "animate-pulse-soft bg-red-600 text-white"
          : isBusy
            ? "cursor-not-allowed bg-room-panelAlt text-room-muted"
            : "bg-room-accent text-room-bg hover:bg-emerald-400"
      }`}
    >
      <MicIcon className="h-5 w-5" />
      {labels[status]}
    </button>
  );
}

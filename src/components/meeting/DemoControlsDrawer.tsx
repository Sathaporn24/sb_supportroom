"use client";

import { useState } from "react";
import type { TutorUserAction } from "@/tutor/intents";

type Props = {
  open: boolean;
  onToggle: () => void;
  sendAction: (action: TutorUserAction) => void;
  submitChatMessage: (text: string) => void;
};

const stateButtons: { label: string; action: TutorUserAction }[] = [
  { label: "พร้อมแล้ว", action: { type: "READY" } },
  { label: "ยังไม่เข้าใจ", action: { type: "NOT_UNDERSTOOD" } },
  { label: "ยังไม่เข้าใจอีกครั้ง", action: { type: "STILL_NOT_UNDERSTOOD" } },
  { label: "ขอดูขั้นตอนก่อนหน้า", action: { type: "REVIEW_PREVIOUS" } },
  { label: "ขอพักก่อน", action: { type: "PAUSE" } },
  { label: "สอนต่อได้เลย", action: { type: "RESUME" } },
  { label: "เสียงรบกวนหรือไม่มีคำพูดที่มีความหมาย", action: { type: "NOISE_OR_MEANINGLESS" } },
  { label: "จำลอง Disconnect", action: { type: "DISCONNECT" } },
];

const questionButtons: { label: string; question: string }[] = [
  { label: "ลืมรหัสผ่านต้องทำอย่างไร", question: "ลืมรหัสผ่านต้องทำอย่างไร" },
  { label: "เลือกโรงเรียนไม่เจอ", question: "เลือกโรงเรียนไม่เจอ" },
  { label: "คำถามเรื่องระบบอื่น", question: "อยากถามเรื่องระบบอื่นที่ไม่เกี่ยวกับการเข้าสู่ระบบ" },
  { label: "คำถามนอกเรื่อง", question: "วันนี้อากาศเป็นอย่างไรบ้างคะ" },
];

export function DemoControlsDrawer({ open, onToggle, sendAction, submitChatMessage }: Props) {
  const [customQuestion, setCustomQuestion] = useState("");

  return (
    <div className="fixed bottom-24 left-4 z-40 sm:bottom-4">
      <button
        onClick={onToggle}
        className="rounded-full border border-amber-500/40 bg-amber-500/10 px-3 py-1.5 text-xs font-medium text-amber-300 shadow-lg hover:bg-amber-500/20"
      >
        {open ? "ปิด Demo Controls" : "Demo Controls"}
      </button>
      {open && (
        <div className="mt-2 w-72 space-y-3 rounded-xl border border-amber-500/30 bg-room-panel p-4 shadow-2xl">
          <p className="text-xs font-semibold uppercase tracking-wide text-amber-300">
            จำลองเหตุการณ์ (Development only)
          </p>
          <div className="flex flex-wrap gap-2">
            {stateButtons.map((btn) => (
              <button
                key={btn.label}
                onClick={() => sendAction(btn.action)}
                className="rounded-md border border-room-border bg-room-panelAlt px-2.5 py-1.5 text-xs text-room-text hover:border-room-accent/60"
              >
                {btn.label}
              </button>
            ))}
          </div>
          <p className="text-xs font-semibold uppercase tracking-wide text-amber-300">คำถามตัวอย่าง</p>
          <div className="flex flex-wrap gap-2">
            {questionButtons.map((btn) => (
              <button
                key={btn.label}
                onClick={() => submitChatMessage(btn.question)}
                className="rounded-md border border-room-border bg-room-panelAlt px-2.5 py-1.5 text-xs text-room-text hover:border-room-accent/60"
              >
                {btn.label}
              </button>
            ))}
          </div>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              if (customQuestion.trim()) {
                submitChatMessage(customQuestion.trim());
                setCustomQuestion("");
              }
            }}
            className="flex gap-2"
          >
            <input
              value={customQuestion}
              onChange={(e) => setCustomQuestion(e.target.value)}
              placeholder="พิมพ์คำถามอื่น..."
              aria-label="พิมพ์คำถามจำลอง"
              className="flex-1 rounded-md border border-room-border bg-room-bg px-2 py-1.5 text-xs text-room-text outline-none focus:border-room-accent"
            />
            <button
              type="submit"
              className="rounded-md bg-amber-500/80 px-2.5 py-1.5 text-xs font-medium text-room-bg hover:bg-amber-400"
            >
              ส่ง
            </button>
          </form>
        </div>
      )}
    </div>
  );
}

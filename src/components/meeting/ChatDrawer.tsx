"use client";

import { useState, type FormEvent } from "react";
import type { SummaryQuestion } from "@/types/domain";

type Props = {
  open: boolean;
  onClose: () => void;
  questions: SummaryQuestion[];
  onSubmit: (text: string) => void;
};

export function ChatDrawer({ open, onClose, questions, onSubmit }: Props) {
  const [draft, setDraft] = useState("");

  if (!open) {
    return null;
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    const trimmed = draft.trim();
    if (!trimmed) {
      return;
    }
    onSubmit(trimmed);
    setDraft("");
  };

  return (
    <div className="fixed inset-x-0 bottom-0 z-30 flex max-h-[70vh] flex-col rounded-t-2xl border border-room-border bg-room-panel shadow-2xl sm:absolute sm:inset-x-auto sm:right-4 sm:bottom-20 sm:h-[420px] sm:max-h-none sm:w-80 sm:rounded-xl">
      <div className="flex items-center justify-between border-b border-room-border px-4 py-3">
        <p className="text-sm font-semibold text-room-text">แชตสำรอง</p>
        <button
          onClick={onClose}
          aria-label="ปิดแชต"
          className="rounded-md px-2 py-1 text-room-muted hover:bg-room-panelAlt hover:text-room-text"
        >
          ปิด
        </button>
      </div>
      <div className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
        {questions.length === 0 && (
          <p className="text-sm text-room-muted">พิมพ์คำถามของคุณครูที่นี่ได้เลยค่ะ</p>
        )}
        {questions.map((q, index) => (
          <div key={index} className="space-y-1">
            <p className="rounded-lg bg-room-panelAlt px-3 py-2 text-sm text-room-text">{q.question}</p>
            {q.answer && (
              <p className="rounded-lg bg-room-accentSoft px-3 py-2 text-sm text-room-text">{q.answer}</p>
            )}
          </div>
        ))}
      </div>
      <form onSubmit={handleSubmit} className="flex gap-2 border-t border-room-border p-3">
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="พิมพ์คำถาม..."
          aria-label="พิมพ์คำถาม"
          className="flex-1 rounded-lg border border-room-border bg-room-bg px-3 py-2 text-sm text-room-text outline-none focus:border-room-accent"
        />
        <button
          type="submit"
          className="rounded-lg bg-room-accent px-3 py-2 text-sm font-medium text-room-bg hover:bg-emerald-400"
        >
          ส่ง
        </button>
      </form>
    </div>
  );
}

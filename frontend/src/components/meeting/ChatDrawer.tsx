"use client";

import { useState } from "react";
import type { ChatMessage, SessionQuestion } from "@/types/domain";
import { answerStatusLabels } from "@/utils/session-status";

type Props = {
  open: boolean;
  onClose: () => void;
  questions: SessionQuestion[];
  chatMessages?: ChatMessage[];
  /** Omit to keep this panel read-only (falls back to the old Q&A-log-only behavior). */
  onSendMessage?: (text: string) => Promise<void>;
};

const senderLabel: Record<ChatMessage["senderRole"], string> = {
  recipient: "ผู้เข้าร่วม",
  agent: "ทีมซัพพอร์ต",
  system: "ระบบ",
};

type TimelineEntry =
  | { kind: "question"; key: string; createdAt: string; question: SessionQuestion }
  | { kind: "chat"; key: string; createdAt: string; message: ChatMessage };

// Push-to-Talk questions and typed chat messages share one time-ordered feed - the drawer
// used to be read-only (Q&A log only); onSendMessage turns it into a real two-way channel.
export function ChatDrawer({ open, onClose, questions, chatMessages = [], onSendMessage }: Props) {
  const [draft, setDraft] = useState("");
  const [sending, setSending] = useState(false);

  if (!open) {
    return null;
  }

  const timeline: TimelineEntry[] = [
    ...questions.map((question, index) => ({
      kind: "question" as const,
      key: `q-${question.id || index}`,
      createdAt: question.createdAt,
      question,
    })),
    ...chatMessages.map((message) => ({
      kind: "chat" as const,
      key: `c-${message.id}`,
      createdAt: message.createdAt,
      message,
    })),
  ].sort((a, b) => a.createdAt.localeCompare(b.createdAt));

  async function handleSend() {
    const text = draft.trim();
    if (!text || !onSendMessage || sending) {
      return;
    }
    setSending(true);
    try {
      await onSendMessage(text);
      setDraft("");
    } catch {
      // Connection hiccup - keep the draft so the sender can retry instead of retyping.
    } finally {
      setSending(false);
    }
  }

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
        {timeline.length === 0 && (
          <p className="text-sm text-room-muted">กดค้างปุ่มไมค์เพื่อถามคำถาม หรือพิมพ์ข้อความได้เลยค่ะ</p>
        )}
        {timeline.map((entry) =>
          entry.kind === "question" ? (
            <div key={entry.key} className="space-y-1">
              {entry.question.transcript && (
                <p className="rounded-lg bg-room-panelAlt px-3 py-2 text-sm text-room-text">
                  {entry.question.transcript}
                  <span className="ml-2 text-xs text-room-muted">({answerStatusLabels[entry.question.answerStatus]})</span>
                </p>
              )}
              {entry.question.answer && (
                <p className="rounded-lg bg-room-accentSoft px-3 py-2 text-sm text-room-text">{entry.question.answer}</p>
              )}
            </div>
          ) : (
            <div key={entry.key} className="space-y-1">
              <p className="text-xs text-room-muted">{entry.message.senderName || senderLabel[entry.message.senderRole]}</p>
              <p className="rounded-lg bg-room-panelAlt px-3 py-2 text-sm text-room-text">{entry.message.text}</p>
            </div>
          ),
        )}
      </div>
      {onSendMessage && (
        <div className="flex shrink-0 items-center gap-2 border-t border-room-border px-3 py-2">
          <input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                void handleSend();
              }
            }}
            placeholder="พิมพ์ข้อความ..."
            className="flex-1 rounded-lg border border-room-border bg-room-panelAlt px-3 py-2 text-sm text-room-text outline-none focus:border-room-accent"
          />
          <button
            onClick={() => void handleSend()}
            disabled={sending || !draft.trim()}
            className="rounded-lg bg-room-accentSoft px-3 py-2 text-sm font-medium text-room-text disabled:opacity-50"
          >
            ส่ง
          </button>
        </div>
      )}
    </div>
  );
}

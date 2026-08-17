"use client";

import { useState } from "react";
import { XIcon } from "lucide-react";
import type { ChatMessage, SessionQuestion } from "@/types/domain";
import { answerStatusLabels } from "@/utils/session-status";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

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
    <div className="fixed inset-x-0 bottom-0 z-30 flex max-h-[70vh] flex-col rounded-t-2xl border bg-card shadow-2xl sm:absolute sm:inset-x-auto sm:right-4 sm:bottom-20 sm:h-[420px] sm:max-h-none sm:w-80 sm:rounded-xl">
      <div className="flex items-center justify-between border-b px-4 py-3">
        <p className="text-sm font-semibold text-foreground">แชตสำรอง</p>
        <Button variant="ghost" size="icon-sm" onClick={onClose} aria-label="ปิดแชต" title="ปิดแชต">
          <XIcon />
        </Button>
      </div>
      <div className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 py-3">
        {timeline.length === 0 && (
          <p className="text-sm text-muted-foreground">กดค้างปุ่มไมค์เพื่อถามคำถาม หรือพิมพ์ข้อความได้เลยค่ะ</p>
        )}
        {timeline.map((entry) =>
          entry.kind === "question" ? (
            <div key={entry.key} className="flex flex-col gap-1">
              {entry.question.transcript && (
                <p className="rounded-lg bg-muted px-3 py-2 text-sm text-foreground">
                  {entry.question.transcript}
                  <span className="ml-2 text-xs text-muted-foreground">
                    ({answerStatusLabels[entry.question.answerStatus]})
                  </span>
                </p>
              )}
              {entry.question.answer && (
                <p className="rounded-lg bg-primary/10 px-3 py-2 text-sm text-foreground">{entry.question.answer}</p>
              )}
            </div>
          ) : (
            <div key={entry.key} className="flex flex-col gap-1">
              <p className="text-xs text-muted-foreground">
                {entry.message.senderName || senderLabel[entry.message.senderRole]}
              </p>
              <p className="rounded-lg bg-muted px-3 py-2 text-sm text-foreground">{entry.message.text}</p>
            </div>
          ),
        )}
      </div>
      {onSendMessage && (
        <div className="flex shrink-0 items-center gap-2 border-t px-3 py-2">
          <Input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                void handleSend();
              }
            }}
            placeholder="พิมพ์ข้อความ..."
            className="flex-1"
          />
          <Button onClick={() => void handleSend()} disabled={sending || !draft.trim()}>
            ส่ง
          </Button>
        </div>
      )}
    </div>
  );
}

import type { SessionQuestion } from "@/types/domain";

type Props = {
  open: boolean;
  onClose: () => void;
  questions: SessionQuestion[];
};

const scopeLabel: Record<SessionQuestion["answerStatus"], string> = {
  answered: "ตอบแล้ว",
  not_found: "ไม่พบข้อมูล",
  out_of_scope: "นอกเรื่อง",
  no_speech: "ไม่มีคำพูด",
  transcription_failed: "ถอดเสียงไม่ได้",
};

// Push-to-Talk is the only way to ask a question now - this panel is a read-only
// transcript/answer log, not a text-input chat.
export function ChatDrawer({ open, onClose, questions }: Props) {
  if (!open) {
    return null;
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
        {questions.length === 0 && (
          <p className="text-sm text-room-muted">กดค้างปุ่มไมค์เพื่อถามคำถามได้เลยค่ะ คำถามและคำตอบจะแสดงที่นี่</p>
        )}
        {questions.map((q, index) => (
          <div key={index} className="space-y-1">
            {q.transcript && (
              <p className="rounded-lg bg-room-panelAlt px-3 py-2 text-sm text-room-text">
                {q.transcript}
                <span className="ml-2 text-xs text-room-muted">({scopeLabel[q.answerStatus]})</span>
              </p>
            )}
            {q.answer && <p className="rounded-lg bg-room-accentSoft px-3 py-2 text-sm text-room-text">{q.answer}</p>}
          </div>
        ))}
      </div>
    </div>
  );
}

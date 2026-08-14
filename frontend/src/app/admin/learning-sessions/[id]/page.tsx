"use client";

import { useEffect, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { answerStatusLabels, reviewResultLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { useAgentSessionChat } from "@/hooks/use-agent-session-chat";
import type { ReviewResult, SessionQuestion, SessionSummary } from "@/types/domain";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { LoadingBlock } from "@/components/ui/LoadingBlock";
import { ChatDrawer } from "@/components/meeting/ChatDrawer";

function mergeQuestions(base: SessionQuestion[], live: SessionQuestion[]): SessionQuestion[] {
  const byId = new Map(base.map((q) => [q.id, q]));
  for (const q of live) {
    // Live questions arrive straight off the wire with no review fields - keep whatever review
    // the agent has already recorded rather than blanking it on every broadcast.
    const existing = byId.get(q.id);
    byId.set(q.id, existing ? { ...q, ...pickReview(existing) } : q);
  }
  return [...byId.values()].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
}

function pickReview(q: SessionQuestion): Partial<SessionQuestion> {
  return q.reviewResult
    ? { reviewResult: q.reviewResult, reviewNote: q.reviewNote, reviewedAt: q.reviewedAt }
    : {};
}

/** One learner's run: the full CS view, including the review controls. */
export default function LearningSessionSummaryPage() {
  const params = useParams<{ id: string }>();
  const [summary, setSummary] = useState<SessionSummary | null | "loading">("loading");
  const [questions, setQuestions] = useState<SessionQuestion[]>([]);
  const [chatOpen, setChatOpen] = useState(false);

  // Real-time so CS sees a session that is still in progress update live, not just after it ends.
  const chat = useAgentSessionChat(params.id);

  useEffect(() => {
    let active = true;
    api
      .getLearningSummaryById(params.id)
      .then(({ summary: found }) => {
        if (!active) return;
        setSummary(found);
        setQuestions(found.questions);
      })
      .catch(() => {
        if (active) setSummary(null);
      });
    return () => {
      active = false;
    };
  }, [params.id]);

  if (summary === "loading") {
    return (
      <main className="p-6">
        <LoadingBlock label="กำลังโหลดสรุปการเรียน..." />
      </main>
    );
  }
  if (!summary) {
    return <main className="p-6 text-room-muted">ไม่พบการเรียนนี้ค่ะ</main>;
  }

  const merged = mergeQuestions(questions, chat.liveQuestions);
  const unresolved = merged
    .filter((q) => q.answerStatus === "not_found")
    .map((q) => q.transcript || q.answer || "")
    .filter(Boolean);

  return (
    <main className="mx-auto max-w-2xl space-y-6 p-6">
      <div className="flex items-start justify-between">
        <div>
          <AdminLink href="/admin" className="text-xs text-room-muted hover:text-room-text">
            ← กลับหน้า Admin
          </AdminLink>
          <h1 className="mt-1 text-xl font-semibold text-room-text">สรุปการเรียน</h1>
        </div>
        <button
          onClick={() => setChatOpen((prev) => !prev)}
          className="rounded-lg border border-room-border bg-room-panel px-3 py-2 text-sm font-medium text-room-text hover:bg-room-panelAlt"
        >
          แชท{chat.chatMessages.length > 0 ? ` (${chat.chatMessages.length})` : ""}
        </button>
      </div>

      <Card className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <p className="text-room-muted">เรียนครบทุกสไลด์หรือไม่</p>
          <p className="text-room-text">{summary.completedAllSlides ? "ครบ" : "ไม่ครบ"}</p>
        </div>
        <div>
          <p className="text-room-muted">สไลด์ล่าสุด</p>
          <p className="text-room-text">{summary.lastSlideObjectId ?? "-"}</p>
        </div>
        <div>
          <p className="text-room-muted">อัปเดตล่าสุด</p>
          <p className="text-room-text">{formatDateTimeTh(summary.createdAt)}</p>
        </div>
        <div>
          <p className="text-room-muted">จำนวนคำถาม</p>
          <p className="text-room-text">{merged.length}</p>
        </div>
      </Card>

      <Card className="space-y-3">
        <h2 className="text-sm font-semibold text-room-text">คำถามระหว่างเรียน (อัปเดตสด)</h2>
        {merged.length === 0 ? (
          <p className="text-sm text-room-muted">ไม่มีคำถามในการเรียนนี้</p>
        ) : (
          <ul className="space-y-3">
            {merged.map((question) => (
              <QuestionReviewItem
                key={question.id}
                question={question}
                onReviewed={(updated) =>
                  setQuestions((prev) => prev.map((q) => (q.id === updated.id ? updated : q)))
                }
              />
            ))}
          </ul>
        )}
      </Card>

      {/* Internal only - CORE_FEATURE_SPEC §2.5 keeps this off the learner's own recap. */}
      <Card className="space-y-2">
        <h2 className="text-sm font-semibold text-room-text">จุดที่ตอบไม่ได้ (รอทีม CS ตรวจสอบ)</h2>
        {unresolved.length === 0 ? (
          <p className="text-sm text-room-muted">ไม่มี</p>
        ) : (
          <ul className="list-inside list-disc text-sm text-room-text">
            {unresolved.map((item, i) => (
              <li key={i}>{item}</li>
            ))}
          </ul>
        )}
      </Card>

      <ChatDrawer
        open={chatOpen}
        onClose={() => setChatOpen(false)}
        questions={[]}
        chatMessages={chat.chatMessages}
        onSendMessage={chat.sendChatMessage}
      />
    </main>
  );
}

function QuestionReviewItem({
  question,
  onReviewed,
}: {
  question: SessionQuestion;
  onReviewed: (updated: SessionQuestion) => void;
}) {
  const [note, setNote] = useState(question.reviewNote ?? "");
  const [saving, setSaving] = useState<ReviewResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function submit(reviewResult: ReviewResult) {
    setSaving(reviewResult);
    setError(null);
    try {
      const { question: updated } = await api.reviewSessionQuestion(question.id, {
        reviewResult,
        reviewNote: note.trim() || undefined,
      });
      onReviewed(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "บันทึกผลการตรวจไม่สำเร็จ");
    } finally {
      setSaving(null);
    }
  }

  return (
    <li className="rounded-lg border border-room-border bg-room-panelAlt p-3 text-sm">
      <p className="text-room-text">
        {question.transcript || "(ไม่มีคำถอดเสียง)"} <Badge tone="info">{answerStatusLabels[question.answerStatus]}</Badge>
        {question.reviewResult && (
          <Badge tone={question.reviewResult === "correct" ? "success" : "danger"}>
            {reviewResultLabels[question.reviewResult]}
          </Badge>
        )}
      </p>
      {question.answer && <p className="mt-1 text-room-muted">คำตอบ: {question.answer}</p>}

      {/* The note is the point of the review. "ตอบผิด" on its own cannot tell a missing document
          apart from a retrieval miss apart from the model answering from general knowledge, and
          those three are fixed in three different places (CORE_FEATURE_SPEC §2.7). */}
      <textarea
        value={note}
        onChange={(e) => setNote(e.target.value)}
        rows={2}
        maxLength={2000}
        placeholder="หมายเหตุ (ไม่บังคับ) — เช่น ไม่มีเอกสารเรื่องนี้ / มีแต่ค้นไม่เจอ / AI เดาเอง"
        className="mt-2 w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-sm text-room-text outline-none focus:border-room-accent"
      />

      {error && <p className="mt-1 text-xs text-red-600">{error}</p>}

      <div className="mt-2 flex flex-wrap items-center gap-2">
        <Button variant="secondary" disabled={saving !== null} onClick={() => void submit("correct")}>
          {saving === "correct" ? "กำลังบันทึก..." : "ตอบถูก"}
        </Button>
        <Button variant="ghost" disabled={saving !== null} onClick={() => void submit("incorrect")}>
          {saving === "incorrect" ? "กำลังบันทึก..." : "ตอบผิด"}
        </Button>
        {question.reviewedAt && (
          <span className="text-xs text-room-muted">ตรวจเมื่อ {formatDateTimeTh(question.reviewedAt)}</span>
        )}
      </div>
    </li>
  );
}

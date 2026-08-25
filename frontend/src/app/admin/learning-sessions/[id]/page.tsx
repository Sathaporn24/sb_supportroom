"use client";

import { useEffect, useState } from "react";
import { AdminLink } from "@/components/admin/AdminLink";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { answerStatusLabels, reviewResultLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { useAgentSessionQuestions } from "@/hooks/use-agent-session-questions";
import type { ReviewResult, SessionQuestion, SessionSummary } from "@/types/domain";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

const STAT_FIELD_COUNT = 4;
const QUESTION_ROW_COUNT = 3;

/** Mirrors this page's own layout (header, stat card, question list, unresolved list) so the
 * loading state doesn't jump once the summary arrives. */
function LearningSessionSummarySkeleton() {
  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div className="flex flex-col gap-2">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="h-6 w-40" />
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-4">
          {Array.from({ length: STAT_FIELD_COUNT }).map((_, index) => (
            <div key={index} className="flex flex-col gap-1">
              <Skeleton className="h-3 w-24" />
              <Skeleton className="h-4 w-16" />
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="flex flex-col gap-3">
          <Skeleton className="h-4 w-48" />
          {Array.from({ length: QUESTION_ROW_COUNT }).map((_, index) => (
            <div key={index} className="flex flex-col gap-2 rounded-lg border bg-muted p-3">
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-2/3" />
            </div>
          ))}
        </CardContent>
      </Card>
    </main>
  );
}

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
  return { reviewResult: q.reviewResult, reviewNote: q.reviewNote, reviewedAt: q.reviewedAt };
}

/** One learner's run: the full CS view, including the review controls. */
export default function LearningSessionSummaryPage() {
  const params = useParams<{ id: string }>();
  const [summary, setSummary] = useState<SessionSummary | null | "loading">("loading");
  const [questions, setQuestions] = useState<SessionQuestion[]>([]);

  // Real-time so CS sees a session that is still in progress update live, not just after it ends.
  const { liveQuestions } = useAgentSessionQuestions(params.id);

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
    return <LearningSessionSummarySkeleton />;
  }
  if (!summary) {
    return <main className="p-6 text-muted-foreground">ไม่พบการเรียนนี้ค่ะ</main>;
  }

  const merged = mergeQuestions(questions, liveQuestions);
  const unresolved = merged
    .filter((q) => q.answerStatus === "not_found")
    .map((q) => q.transcript || q.answer || "")
    .filter(Boolean);

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div className="flex items-start justify-between">
        <div>
          <AdminLink
            href="/admin"
            className="text-xs text-muted-foreground hover:text-foreground"
            data-testid="session-summary-back-to-admin-link"
          >
            ← กลับหน้า Admin
          </AdminLink>
          <h1 className="mt-1 text-xl font-semibold text-primary">สรุปการเรียน</h1>
        </div>
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <p className="text-muted-foreground">เรียนครบทุกสไลด์หรือไม่</p>
            <p>{summary.completedAllSlides ? "ครบ" : "ไม่ครบ"}</p>
          </div>
          <div>
            <p className="text-muted-foreground">สไลด์ล่าสุด</p>
            <p>{summary.lastSlideObjectId ?? "-"}</p>
          </div>
          <div>
            <p className="text-muted-foreground">อัปเดตล่าสุด</p>
            <p>{formatDateTimeTh(summary.createdAt)}</p>
          </div>
          <div>
            <p className="text-muted-foreground">จำนวนคำถาม</p>
            <p>{merged.length}</p>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="flex flex-col gap-3">
          <h2 className="text-sm font-semibold">คำถามระหว่างเรียน (อัปเดตสด)</h2>
          {merged.length === 0 ? (
            <p className="text-sm text-muted-foreground">ไม่มีคำถามในการเรียนนี้</p>
          ) : (
            <ul className="flex flex-col gap-3">
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
        </CardContent>
      </Card>

      {/* Internal only - CORE_FEATURE_SPEC §2.5 keeps this off the learner's own recap. */}
      <Card>
        <CardContent className="flex flex-col gap-2">
          <h2 className="text-sm font-semibold">จุดที่ตอบไม่ได้ (รอทีม CS ตรวจสอบ)</h2>
          {unresolved.length === 0 ? (
            <p className="text-sm text-muted-foreground">ไม่มี</p>
          ) : (
            <ul className="list-inside list-disc text-sm">
              {unresolved.map((item, i) => (
                <li key={i}>{item}</li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
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
  const [saving, setSaving] = useState<ReviewResult | "clear" | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function submit(reviewResult: ReviewResult | null) {
    setSaving(reviewResult ?? "clear");
    setError(null);
    try {
      const { question: updated } = await api.reviewSessionQuestion(question.id, {
        reviewResult,
        reviewNote: reviewResult === null ? undefined : note.trim() || undefined,
      });
      setNote(updated.reviewNote ?? "");
      onReviewed(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "บันทึกผลการตรวจไม่สำเร็จ");
    } finally {
      setSaving(null);
    }
  }

  return (
    <li className="rounded-lg border bg-muted p-3 text-sm" data-testid={`session-question-row-${question.id}`}>
      <p className="flex flex-wrap items-center gap-2">
        <span>{question.transcript || "(ไม่มีคำถอดเสียง)"}</span>
        <Badge variant="secondary">{answerStatusLabels[question.answerStatus]}</Badge>
        {question.reviewResult && (
          <Badge variant={question.reviewResult === "correct" ? "default" : "destructive"}>
            {reviewResultLabels[question.reviewResult]}
          </Badge>
        )}
      </p>
      {question.answer && <p className="mt-1 text-muted-foreground">คำตอบ: {question.answer}</p>}

      {/* The note is the point of the review. "ตอบผิด" on its own cannot tell a missing document
          apart from a retrieval miss apart from the model answering from general knowledge, and
          those three are fixed in three different places (CORE_FEATURE_SPEC §2.7). */}
      <textarea
        value={note}
        onChange={(e) => setNote(e.target.value)}
        rows={2}
        maxLength={2000}
        placeholder="หมายเหตุ (ไม่บังคับ) — เช่น ไม่มีเอกสารเรื่องนี้ / มีแต่ค้นไม่เจอ / AI เดาเอง"
        className="mt-2 w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
        data-testid={`session-question-row-${question.id}-review-note-input`}
      />

      {error && <p className="mt-1 text-xs text-destructive">{error}</p>}

      <div className="mt-2 flex flex-wrap items-center gap-2">
        <Button
          variant="secondary"
          disabled={saving !== null}
          onClick={() => void submit("correct")}
          data-testid={`session-question-row-${question.id}-mark-correct-button`}
        >
          {saving === "correct" ? "กำลังบันทึก..." : "ตอบถูก"}
        </Button>
        <Button
          variant="ghost"
          disabled={saving !== null}
          onClick={() => void submit("incorrect")}
          data-testid={`session-question-row-${question.id}-mark-incorrect-button`}
        >
          {saving === "incorrect" ? "กำลังบันทึก..." : "ตอบผิด"}
        </Button>
        {question.reviewResult && (
          <Button
            variant="outline"
            disabled={saving !== null}
            onClick={() => void submit(null)}
            data-testid={`session-question-row-${question.id}-clear-review-button`}
          >
            {saving === "clear" ? "กำลังล้าง..." : "ล้างผลรีวิว"}
          </Button>
        )}
        {question.reviewedAt && (
          <span className="text-xs text-muted-foreground">ตรวจเมื่อ {formatDateTimeTh(question.reviewedAt)}</span>
        )}
      </div>
    </li>
  );
}

"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { answerStatusLabels } from "@/utils/session-status";
import { getLearnerName, peekLearnerKey } from "@/utils/learner-key";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { LoadingBlock } from "@/components/ui/LoadingBlock";
import type { SessionQuestion } from "@/types/domain";

// No link back to /admin here - this page is reached by anyone holding a public, unauthenticated
// link, and /admin has no auth of its own (see CLAUDE.md). Linking to it from a public-facing
// page handed every learner a one-click path into the full CS dashboard (every session's learner
// name/organization, chat transcripts, and the "Reset Demo Data" button).
export default function SessionEndedPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [questions, setQuestions] = useState<SessionQuestion[] | null>(null);
  const [restarting, setRestarting] = useState(false);

  useEffect(() => {
    const learnerKey = peekLearnerKey(params.token);
    if (!learnerKey) {
      // Nothing to show without a key - there is no "my questions" to look up.
      setQuestions([]);
      return;
    }
    let active = true;
    api
      .getOwnLearningSummary(params.token, learnerKey)
      .then(({ summary }) => {
        if (active) setQuestions(summary.questions);
      })
      .catch(() => {
        // The thank-you still stands on its own if the recap can't be loaded.
        if (active) setQuestions([]);
      });
    return () => {
      active = false;
    };
  }, [params.token]);

  async function handleRestart() {
    const learnerKey = peekLearnerKey(params.token);
    const name = getLearnerName(params.token);
    if (!learnerKey || !name) {
      router.push(`/join/${params.token}`);
      return;
    }
    setRestarting(true);
    try {
      // A new round, not a reopen of the finished one - the old session and its questions stay
      // exactly as they were (CORE_FEATURE_SPEC §2.5).
      await api.restartLearningSession(params.token, { recipientName: name, learnerKey });
      router.push(`/room/${params.token}`);
    } catch {
      setRestarting(false);
      router.push(`/join/${params.token}`);
    }
  }

  if (questions === null) {
    return (
      <main className="flex min-h-screen items-center justify-center p-6">
        <LoadingBlock label="กำลังโหลดสรุปการเรียน..." />
      </main>
    );
  }

  // Only what the learner themselves asked. The "จุดที่ AI ตอบไม่ได้ รอ CS ตรวจสอบ" list is
  // internal and stays on the CS side (CORE_FEATURE_SPEC §2.5) - summary.unansweredPoints is
  // deliberately not read here.
  const answered = questions.filter((q) => q.answerStatus !== "no_speech");

  return (
    <main className="flex min-h-screen items-start justify-center p-6">
      <Card className="w-full max-w-2xl space-y-5">
        <div className="text-center">
          <h1 className="text-xl font-semibold text-room-text">เรียนจบแล้ว ขอบคุณค่ะ</h1>
          <p className="mt-2 text-sm text-room-muted">
            หากมีคำถามเพิ่มเติม สามารถติดต่อทีม CS ได้เลยค่ะ
          </p>
        </div>

        {answered.length > 0 && (
          <div className="space-y-3">
            <h2 className="text-sm font-semibold text-room-text">คำถามของคุณระหว่างเรียน</h2>
            <ul className="space-y-3">
              {answered.map((question) => (
                <li key={question.id} className="rounded-lg border border-room-border bg-room-panelAlt p-3">
                  <p className="text-sm font-medium text-room-text">{question.transcript || "(ไม่มีข้อความถอดเสียง)"}</p>
                  {question.answer && <p className="mt-1 text-sm text-room-muted">{question.answer}</p>}
                  <p className="mt-2 text-xs text-room-muted">{answerStatusLabels[question.answerStatus]}</p>
                </li>
              ))}
            </ul>
          </div>
        )}

        <Button className="w-full" disabled={restarting} onClick={() => void handleRestart()}>
          {restarting ? "กำลังเริ่มรอบใหม่..." : "เรียนอีกครั้ง"}
        </Button>
      </Card>
    </main>
  );
}

"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { answerStatusLabels } from "@/utils/session-status";
import { peekLearnerKey } from "@/utils/learner-key";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import type { LearnerSessionQuestion } from "@/types/domain";

// No link back to /admin here - this page is reached by anyone holding a public, unauthenticated
// link. /admin has JWT auth + RBAC now, but this page still has no reason to advertise it to
// every learner: a link here would still hand out the dashboard's URL to a public-facing
// audience, one login prompt away from every session's learner name/organization, question
// transcripts, and the "Reset Demo Data" button.
export default function SessionEndedPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [questions, setQuestions] = useState<LearnerSessionQuestion[] | null>(null);

  useEffect(() => {
    const learnerKey = peekLearnerKey();
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

  if (questions === null) {
    return (
      <main className="flex min-h-[100dvh] items-center justify-center p-6">
        <LoadingBlock label="กำลังโหลดสรุปการเรียน..." />
      </main>
    );
  }

  // Only what the learner themselves asked. The "จุดที่ AI ตอบไม่ได้ รอ CS ตรวจสอบ" list is
  // internal and stays on the CS side (CORE_FEATURE_SPEC §2.5); the public API shape omits it.
  const answered = questions.filter((q) => q.answerStatus !== "no_speech");

  return (
    <main className="flex min-h-[100dvh] items-start justify-center p-6">
      <Card className="w-full max-w-2xl">
        <CardContent className="flex flex-col gap-5">
          <div className="text-center">
            <h1 className="text-xl font-semibold">เรียนจบแล้ว ขอบคุณค่ะ</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              หากมีคำถามเพิ่มเติม สามารถติดต่อทีม CS ได้เลยค่ะ
            </p>
          </div>

          {answered.length > 0 && (
            <div className="flex flex-col gap-3">
              <h2 className="text-sm font-semibold">คำถามของคุณระหว่างเรียน</h2>
              <ul className="flex flex-col gap-3">
                {answered.map((question) => (
                  <li key={question.id} className="rounded-lg border bg-muted p-3">
                    <p className="text-sm font-medium">{question.transcript || "(ไม่มีข้อความถอดเสียง)"}</p>
                    {question.answer && <p className="mt-1 text-sm text-muted-foreground">{question.answer}</p>}
                    <p className="mt-2 text-xs text-muted-foreground">{answerStatusLabels[question.answerStatus]}</p>
                  </li>
                ))}
              </ul>
            </div>
          )}

          <Button
            className="h-11 w-full"
            data-testid="session-ended-restart-button"
            onClick={() => router.push(`/join/${params.token}`)}
          >
            เรียนอีกครั้ง
          </Button>
        </CardContent>
      </Card>
    </main>
  );
}

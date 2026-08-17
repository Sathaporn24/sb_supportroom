"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { answerStatusLabels, getSessionStatus, sessionStatusLabels } from "@/utils/session-status";
import { formatDateTimeTh } from "@/utils/format";
import { useSessionChat } from "@/hooks/use-session-chat";
import type { SessionQuestion, SessionSummary, TrainingSession } from "@/types/domain";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import { ChatDrawer } from "@/components/meeting/ChatDrawer";

function mergeQuestions(base: SessionQuestion[], live: SessionQuestion[]): SessionQuestion[] {
  const byId = new Map(base.map((q) => [q.id, q]));
  for (const q of live) {
    byId.set(q.id, q);
  }
  return [...byId.values()].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
}

export default function SessionSummaryPage() {
  const params = useParams<{ token: string }>();
  const [session, setSession] = useState<TrainingSession | null | "loading">("loading");
  const [summary, setSummary] = useState<SessionSummary | null>(null);
  const [fallbackQuestions, setFallbackQuestions] = useState<SessionQuestion[]>([]);
  const [chatOpen, setChatOpen] = useState(false);

  // Real-time so CS sees a session that's still in progress update live, not just the
  // post-hoc summary this page originally showed.
  const chat = useSessionChat(params.token, "agent", "ทีมซัพพอร์ต");

  useEffect(() => {
    void api
      .getSessionSummary(params.token)
      .then(async ({ session: found, summary: foundSummary }) => {
        setSession(found);
        setSummary(foundSummary);
        if (!foundSummary) {
          const { questions } = await api.listSessionQuestions(params.token);
          setFallbackQuestions(questions);
        }
      })
      .catch(() => setSession(null));
  }, [params.token]);

  if (session === "loading") {
    return (
      <main className="p-6">
        <LoadingBlock label="กำลังโหลดข้อมูลสรุปผลการสอน..." />
      </main>
    );
  }
  if (!session) {
    return <main className="p-6 text-muted-foreground">ไม่พบ Session นี้ค่ะ</main>;
  }

  const status = getSessionStatus(session);
  const questions = mergeQuestions(summary?.questions ?? fallbackQuestions, chat.liveQuestions);
  const unresolvedItems =
    summary?.unansweredPoints ?? questions.filter((q) => q.answerStatus === "not_found").map((q) => q.transcript || q.answer || "");

  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-6 p-6">
      <div className="flex items-start justify-between">
        <div>
          <Link href="/admin" className="text-xs text-muted-foreground hover:text-foreground">
            ← กลับหน้า Admin
          </Link>
          <h1 className="mt-1 text-xl font-semibold">สรุปผลการสอน</h1>
          <p className="text-sm text-muted-foreground">
            {session.recipientName || "ไม่ระบุชื่อผู้รับลิงก์"} · {session.recipientOrgName || "ไม่ระบุองค์กร"}
          </p>
        </div>
        <Button variant="outline" onClick={() => setChatOpen((prev) => !prev)}>
          แชท{chat.chatMessages.length > 0 ? ` (${chat.chatMessages.length})` : ""}
        </Button>
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-4 text-sm">
          <div className="flex flex-col items-start gap-1">
            <p className="text-muted-foreground">สถานะ</p>
            <Badge
              variant={status === "IN_PROGRESS" ? "default" : status === "EXPIRED" ? "destructive" : "secondary"}
            >
              {sessionStatusLabels[status]}
            </Badge>
          </div>
          <div className="flex flex-col gap-1">
            <p className="text-muted-foreground">สอนครบทุก Slide หรือไม่</p>
            <p>{summary ? (summary.completedAllSlides ? "ครบ" : "ไม่ครบ") : "ยังไม่จบ Session"}</p>
          </div>
          <div className="flex flex-col gap-1">
            <p className="text-muted-foreground">Slide ล่าสุด</p>
            <p>{summary?.lastSlideObjectId ?? session.lastSlideObjectId ?? "-"}</p>
          </div>
          <div className="flex flex-col gap-1">
            <p className="text-muted-foreground">เวลาเริ่ม - จบ</p>
            <p>
              {formatDateTimeTh(session.startedAt)} - {formatDateTimeTh(session.endedAt)}
            </p>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">คำถามที่ถามระหว่างการสอน{!summary && " (อัปเดตสด)"}</CardTitle>
        </CardHeader>
        <CardContent>
          {questions.length === 0 ? (
            <p className="text-sm text-muted-foreground">ไม่มีคำถามในเซสชันนี้</p>
          ) : (
            <ul className="flex flex-col gap-2">
              {questions.map((q, i) => (
                <li key={q.id ?? i} className="rounded-lg border bg-muted p-3 text-sm">
                  <p>
                    {q.transcript || "(ไม่มีคำถอดเสียง)"}{" "}
                    <Badge variant="outline">{answerStatusLabels[q.answerStatus]}</Badge>
                  </p>
                  {q.answer && <p className="mt-1 text-muted-foreground">คำตอบ: {q.answer}</p>}
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">จุดที่ตอบไม่ได้ (รอทีม CS ตรวจสอบ)</CardTitle>
        </CardHeader>
        <CardContent>
          {unresolvedItems.length === 0 ? (
            <p className="text-sm text-muted-foreground">ไม่มี</p>
          ) : (
            <ul className="list-inside list-disc text-sm">
              {unresolvedItems.map((item, i) => (
                <li key={i}>{item}</li>
              ))}
            </ul>
          )}
        </CardContent>
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

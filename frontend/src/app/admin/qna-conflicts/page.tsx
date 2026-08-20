"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeQnAConflict } from "@/types/domain";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { CardListSkeleton } from "@/components/shared/CardListSkeleton";
import { formatDateTimeTh } from "@/utils/format";

/**
 * QQ-9/QQ-10/R5.5 - raised when the model reports a Q&A it retrieved conflicting with a
 * document/slide. The document already won when answering; the follow-up action here is fixing
 * that document, not writing another answer - which is why this is its own page, not a badge on
 * the review queue.
 */
export default function QnaConflictsPage() {
  const [conflicts, setConflicts] = useState<KnowledgeQnAConflict[] | null>(null);
  const [resolvingId, setResolvingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    try {
      const { conflicts: list } = await api.listQnaConflicts();
      setConflicts(list);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "โหลดรายการธงขัดแย้งไม่สำเร็จ");
    }
  }

  useEffect(() => {
    void reload();
  }, []);

  async function handleResolve(id: string) {
    setResolvingId(id);
    setError(null);
    try {
      await api.resolveQnaConflict(id);
      await reload();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "ปิดธงไม่สำเร็จ");
    } finally {
      setResolvingId(null);
    }
  }

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold">ธงขัดแย้ง Q&amp;A กับเอกสาร</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          เกิดเมื่อ AI พบว่า Q&amp;A ที่หยิบมาขัดกับเอกสาร/สไลด์ — AI ตอบตามเอกสารไปแล้ว แถวนี้มีไว้ให้ไปแก้เอกสารต้นเหตุ
          ไม่ใช่การแจ้งว่า Q&amp;A ผิด
        </p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      {!conflicts ? (
        <CardListSkeleton />
      ) : conflicts.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ไม่มีธงค้าง</EmptyTitle>
            <EmptyDescription>ยังไม่มี Q&amp;A ใดขัดกับเอกสาร</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="flex flex-col gap-3">
          {conflicts.map((conflict) => (
            <Card key={conflict.id} size="sm">
              <CardContent className="flex flex-col gap-2">
                <p className="text-sm">
                  ขัดกับ: <span className="font-medium">{conflict.conflictingSourceLabel}</span>
                </p>
                {conflict.modelNote && <p className="text-sm text-muted-foreground">{conflict.modelNote}</p>}
                <div className="flex items-center justify-between">
                  <p className="text-xs text-muted-foreground">{formatDateTimeTh(conflict.createdAt)}</p>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => handleResolve(conflict.id)}
                    disabled={resolvingId === conflict.id}
                  >
                    {resolvingId === conflict.id ? <Spinner /> : "ปิดธง"}
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </main>
  );
}

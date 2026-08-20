"use client";

import { useEffect, useState } from "react";
import { KnowledgeQnAAnswerDialog } from "@/components/admin/KnowledgeQnAAnswerDialog";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeQnAQueueItem } from "@/types/domain";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import { formatDateTimeTh } from "@/utils/format";

/**
 * P8/R5.1 - cross-session, cross-lesson queue of questions with no correct answer yet. A row
 * leaves this list only by saving a Q&A against it (QQ-2/QQ-7) - there is no "dismiss" button by
 * design (design.md DM-9/Q6): a question CS genuinely can't answer just stays here.
 */
export default function QnaQueuePage() {
  const [queue, setQueue] = useState<KnowledgeQnAQueueItem[] | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [dialogOpen, setDialogOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function reload() {
    try {
      const { queue: list } = await api.getQnaQueue();
      setQueue(list);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "โหลดคิวไม่สำเร็จ");
    }
  }

  useEffect(() => {
    void reload();
  }, []);

  function toggleSelected(id: string, checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  function handleSaved() {
    setDialogOpen(false);
    setSelectedIds(new Set());
    void reload();
  }

  const selectedItems = (queue ?? []).filter((item) => selectedIds.has(item.id));

  return (
    <main className="mx-auto flex max-w-4xl flex-col gap-6 p-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold">คิวคำถามที่ยังไม่มีคำตอบ</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            รวมคำถามที่ AI ตอบไม่ได้ (not_found) และคำถามที่ CS ตรวจแล้วพบว่า AI ตอบผิด ข้ามทุกการเรียนและทุกบทเรียน —
            เลือกคำถามที่ตอบด้วยคำตอบเดียวกันได้พร้อมกัน
          </p>
        </div>
        <Button onClick={() => setDialogOpen(true)} disabled={selectedIds.size === 0}>
          เขียนคำตอบ ({selectedIds.size})
        </Button>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      {!queue ? (
        <LoadingBlock label="กำลังโหลดคิว..." />
      ) : queue.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ไม่มีคำถามค้างในคิว</EmptyTitle>
            <EmptyDescription>ทุกคำถามมีคำตอบที่ถูกแล้ว</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[720px]">
            <TableHeader>
              <TableRow>
                <TableHead className="w-10 px-4" />
                <TableHead className="px-4">คำถาม</TableHead>
                <TableHead className="px-4">บทเรียน</TableHead>
                <TableHead className="px-4">แหล่งที่มา</TableHead>
                <TableHead className="px-4">เมื่อ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {queue.map((item) => (
                <TableRow key={item.id}>
                  <TableCell className="px-4 py-3">
                    <Checkbox
                      checked={selectedIds.has(item.id)}
                      onCheckedChange={(checked) => toggleSelected(item.id, checked === true)}
                    />
                  </TableCell>
                  <TableCell className="px-4 py-3">
                    <p>{item.transcript || <span className="text-muted-foreground">(ไม่มีข้อความ)</span>}</p>
                    {item.answer && (
                      <p className="mt-1 text-xs text-muted-foreground">AI ตอบว่า: {item.answer}</p>
                    )}
                  </TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{item.lessonSlug ?? "-"}</TableCell>
                  <TableCell className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {item.fromNotFound && <Badge variant="outline">AI ไม่มีข้อมูล</Badge>}
                      {item.fromIncorrect && <Badge variant="destructive">CS ตรวจว่าตอบผิด</Badge>}
                    </div>
                  </TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{formatDateTimeTh(item.createdAt)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <KnowledgeQnAAnswerDialog
        open={dialogOpen}
        items={selectedItems}
        onClose={() => setDialogOpen(false)}
        onSaved={handleSaved}
      />
    </main>
  );
}

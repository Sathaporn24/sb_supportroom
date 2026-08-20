"use client";

import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { DocumentChunk } from "@/types/domain";
import { AdminLink } from "@/components/admin/AdminLink";
import { Badge } from "@/components/ui/badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import { cn } from "@/lib/utils";

/**
 * DI-7 - shows CS exactly what the knowledge store received for this document, one row per
 * chunk, ordered by seqNo. Rows flagged hasSuspectCharacters (DI-6) sort first so a Thai file
 * with mangled diacritics is spotted without scrolling the whole document - "สายตาคนคือทางเดียว
 * ที่จับเคสนี้ได้" (R6.3), the flag is only a sort hint, never a correctness verdict.
 */
export default function DocumentChunksPage() {
  const params = useParams<{ id: string }>();
  const searchParams = useSearchParams();
  const fileName = searchParams.get("fileName");

  const [chunks, setChunks] = useState<DocumentChunk[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .getDocumentChunks(params.id)
      .then(({ chunks: list }) => setChunks(list))
      .catch((err) => setError(err instanceof ApiClientError ? err.response.error.message : "โหลดข้อความไม่สำเร็จ"));
  }, [params.id]);

  const sorted = chunks
    ? [...chunks].sort((a, b) => Number(b.hasSuspectCharacters) - Number(a.hasSuspectCharacters) || a.seqNo - b.seqNo)
    : null;

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <AdminLink href="/admin/documents" className="text-xs text-muted-foreground hover:text-foreground">
          ← กลับคลังเอกสาร
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold">ข้อความที่แปลงได้{fileName ? `: ${fileName}` : ""}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          นี่คือข้อความชุดเดียวกับที่คลังความรู้ใช้ตอบคำถามจริง — ไม่ใช่ข้อความที่แปลงสดใหม่ทุกครั้ง
          หากพบตัวอักษรเพี้ยน (เช่น วรรณยุกต์หาย) ให้ลบเอกสารแล้วอัปโหลดไฟล์ใหม่
        </p>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      {!sorted ? (
        <LoadingBlock label="กำลังโหลดข้อความ..." />
      ) : sorted.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>แปลงไม่ได้</EmptyTitle>
            <EmptyDescription>
              เอกสารนี้ยังไม่มีข้อความที่แปลงได้ — อาจกำลังประมวลผลอยู่ ไม่มีข้อความในไฟล์ (เช่นไฟล์สแกน)
              หรือแปลงไฟล์ไม่สำเร็จ ดูสถานะที่หน้ารายการเอกสารเพื่อทราบสาเหตุ
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table className="min-w-[560px]">
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">ลำดับ</TableHead>
                <TableHead className="px-4">Chunk</TableHead>
                <TableHead className="px-4">ตัวอักษร</TableHead>
                <TableHead className="px-4">ข้อความ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {sorted.map((chunk) => (
                <TableRow
                  key={chunk.id}
                  className={cn(chunk.hasSuspectCharacters && "bg-destructive/5")}
                >
                  <TableCell className="px-4 py-3 text-muted-foreground">{chunk.seqNo}</TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">
                    {chunk.chunkKey}
                    {chunk.hasSuspectCharacters && (
                      <Badge variant="destructive" className="ml-2">
                        ตัวอักษรน่าสงสัย
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell className="px-4 py-3 text-muted-foreground">{chunk.charCount}</TableCell>
                  <TableCell className="px-4 py-3 whitespace-pre-wrap">{chunk.text}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </main>
  );
}

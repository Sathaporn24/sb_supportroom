"use client";

import { useEffect, useState } from "react";
import { PlusIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory } from "@/types/domain";
import { AdminLink } from "@/components/admin/AdminLink";
import { CategoryFormDialog } from "@/components/admin/CategoryFormDialog";
import { CategoryTree } from "@/components/admin/CategoryTree";
import { Button } from "@/components/ui/button";
import { LoadingBlock } from "@/components/shared/LoadingBlock";

type DialogState =
  | { mode: "create-parent" }
  | { mode: "create-child"; parent: KnowledgeCategory }
  | { mode: "edit"; category: KnowledgeCategory }
  | null;

export default function CategoriesPage() {
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);

  async function reload() {
    setLoading(true);
    try {
      const { categories: list } = await api.listKnowledgeCategories();
      setCategories(list);
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "โหลดรายการหมวดไม่สำเร็จ");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void reload();
  }, []);

  function handleSaved(category: KnowledgeCategory) {
    setCategories((prev) => {
      const next = prev.filter((c) => c.id !== category.id);
      return [...next, category];
    });
  }

  async function handleDelete(category: KnowledgeCategory) {
    const confirmed = window.confirm(`ต้องการลบหมวด "${category.name}" ใช่หรือไม่?`);
    if (!confirmed) return;
    setError(null);
    try {
      await api.deleteKnowledgeCategory(category.id);
      setCategories((prev) => prev.filter((c) => c.id !== category.id));
    } catch (err) {
      // TX-6 blocks deletion with an explicit count of lessons/documents/Q&A/subcategories
      // still inside - that message must reach the screen verbatim, not a generic fallback.
      setError(err instanceof ApiClientError ? err.response.error.message : "ลบหมวดไม่สำเร็จ");
    }
  }

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <AdminLink href="/admin" className="text-xs text-muted-foreground hover:text-foreground">
            ← กลับหน้าแรก
          </AdminLink>
          <h1 className="mt-1 text-xl font-semibold">จัดการหมวดความรู้</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            หมวดใหญ่และหมวดย่อย 2 ชั้น ใช้จัดกลุ่มบทเรียน เอกสาร และคำถาม-คำตอบ
          </p>
        </div>
        <Button onClick={() => setDialog({ mode: "create-parent" })}>
          <PlusIcon data-icon="inline-start" />
          เพิ่มหมวดใหญ่
        </Button>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      {loading ? (
        <LoadingBlock label="กำลังโหลดหมวด..." />
      ) : (
        <CategoryTree
          categories={categories}
          onAddChild={(parent) => setDialog({ mode: "create-child", parent })}
          onEdit={(category) => setDialog({ mode: "edit", category })}
          onDelete={handleDelete}
        />
      )}

      <CategoryFormDialog
        open={dialog !== null}
        onClose={() => setDialog(null)}
        onSaved={handleSaved}
        parentId={dialog?.mode === "create-child" ? dialog.parent.id : undefined}
        editing={dialog?.mode === "edit" ? dialog.category : null}
      />
    </main>
  );
}

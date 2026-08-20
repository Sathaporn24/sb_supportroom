"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory } from "@/types/domain";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";

type Props = {
  open: boolean;
  onClose: () => void;
  onSaved: (category: KnowledgeCategory) => void;
  /** Present when creating a subcategory under this parent; omit for a top-level category. */
  parentId?: string;
  /** Present when renaming an existing category instead of creating a new one. */
  editing?: KnowledgeCategory | null;
};

/** Same dialog handles create (top-level or subcategory) and rename - TX-1..TX-3 are all
 * enforced server-side, this just surfaces the Thai error message the API returns. */
export function CategoryFormDialog({ open, onClose, onSaved, parentId, editing }: Props) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [sortOrder, setSortOrder] = useState("0");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    setName(editing?.name ?? "");
    setDescription(editing?.description ?? "");
    setSortOrder(String(editing?.sortOrder ?? 0));
    setError(null);
    setSaving(false);
  }, [open, editing]);

  async function handleSave() {
    if (!name.trim()) {
      setError("ต้องระบุชื่อหมวด");
      return;
    }
    setError(null);
    setSaving(true);
    try {
      const input = {
        name: name.trim(),
        description: description.trim() || undefined,
        sortOrder: Number(sortOrder) || 0,
      };
      const { category } = editing
        ? await api.updateKnowledgeCategory(editing.id, input)
        : await api.createKnowledgeCategory({ ...input, parentId });
      onSaved(category);
      onClose();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "บันทึกหมวดไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  const title = editing ? "แก้ไขหมวด" : parentId ? "เพิ่มหมวดย่อย" : "เพิ่มหมวดใหญ่";

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          {error && <p className="text-xs text-destructive">{error}</p>}
          <div className="flex flex-col gap-2">
            <Label htmlFor="category-name">ชื่อหมวด</Label>
            <Input id="category-name" value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="category-description">คำอธิบาย (ไม่บังคับ)</Label>
            <Input
              id="category-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="category-sort-order">ลำดับในเมนู (เลขน้อยขึ้นก่อน)</Label>
            <Input
              id="category-sort-order"
              type="number"
              value={sortOrder}
              onChange={(e) => setSortOrder(e.target.value)}
            />
          </div>
          <Button onClick={handleSave} disabled={saving}>
            {saving ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังบันทึก...
              </>
            ) : (
              "บันทึก"
            )}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

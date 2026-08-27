"use client";

import { useEffect, useState } from "react";
import { PlusIcon, Trash2Icon } from "lucide-react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory } from "@/types/domain";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { toast } from "@/components/ui/toast";
import { cn } from "@/lib/utils";

type SubcategoryRow = {
  key: string;
  category?: KnowledgeCategory;
  name: string;
};

type StepState = "done" | "active" | "upcoming";

function StepBadge({ stepNumber, label, state }: { stepNumber: number; label: string; state: StepState }) {
  return (
    <div className="flex gap-2 h-9 items-center min-w-[128px] px-2.5 py-2 rounded-md">
      {state === "done" ? (
        <div className="flex items-center justify-center size-6 rounded-full bg-[#e86a27] text-white text-[10px] font-bold">
          ✓
        </div>
      ) : state === "active" ? (
        <div className="flex items-center justify-center size-5 rounded-full bg-[#ffeee0] border border-[#e86a27] text-primary text-xs font-bold">
          {stepNumber}
        </div>
      ) : (
        <div className="flex items-center justify-center size-5 rounded-full bg-[#f5f3ff] border border-[#d1d1d6] text-muted-foreground text-xs font-bold">
          {stepNumber}
        </div>
      )}
      <span
        className={cn(
          "text-xs leading-[18px]",
          state === "upcoming" ? "text-muted-foreground" : "text-foreground",
        )}
      >
        {label}
      </span>
    </div>
  );
}

type Props = {
  open: boolean;
  onClose: () => void;
  categories: KnowledgeCategory[];
  editingParent?: KnowledgeCategory | null;
};

let nextTemporaryRowId = 0;

function createEmptyRow(): SubcategoryRow {
  nextTemporaryRowId += 1;
  return { key: `new-subcategory-${nextTemporaryRowId}`, name: "" };
}

export function CategoryFormDialog({
  open,
  onClose,
  categories,
  editingParent,
}: Props) {
  const [step, setStep] = useState<1 | 2>(1);
  const [parentName, setParentName] = useState("");
  const [rows, setRows] = useState<SubcategoryRow[]>([]);
  const [persistedParent, setPersistedParent] = useState<KnowledgeCategory | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  // CD-2/CD-4 - yes/no confirm for deleting an already-persisted subcategory row, replacing
  // window.confirm. Holds the row itself (not a closure) so the confirm button can act on it.
  const [pendingRemoval, setPendingRemoval] = useState<SubcategoryRow | null>(null);

  const isEditing = Boolean(editingParent);

  useEffect(() => {
    if (!open) return;
    const children = editingParent
      ? categories
          .filter((category) => category.level === 2 && category.parentId === editingParent.id)
          .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"))
      : [];

    setStep(1);
    setParentName(editingParent?.name ?? "");
    setPersistedParent(editingParent ?? null);
    setRows(
      children.length > 0
        ? children.map((category) => ({ key: category.id, category, name: category.name }))
        : [createEmptyRow()],
    );
    setError(null);
    setSaving(false);
  }, [categories, editingParent, open]);

  function handleNext() {
    if (!parentName.trim()) {
      setError("ต้องระบุชื่อหมวดหมู่");
      return;
    }
    setError(null);
    setStep(2);
  }

  function updateRow(key: string, name: string) {
    setRows((current) => current.map((row) => (row.key === key ? { ...row, name } : row)));
    setError(null);
  }

  function requestRemoveRow(row: SubcategoryRow) {
    if (!row.category) {
      setRows((current) => {
        const next = current.filter((item) => item.key !== row.key);
        return next.length > 0 ? next : [createEmptyRow()];
      });
      return;
    }
    setPendingRemoval(row);
  }

  async function confirmRemoveRow() {
    const row = pendingRemoval;
    if (!row?.category) return;
    setPendingRemoval(null);
    setSaving(true);
    setError(null);
    try {
      await api.deleteKnowledgeCategory(row.category.id);
      setRows((current) => {
        const next = current.filter((item) => item.key !== row.key);
        return next.length > 0 ? next : [createEmptyRow()];
      });
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.response.error.message : "ลบหมวดหมู่ย่อยไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  async function handleConfirm() {
    const trimmedParentName = parentName.trim();
    if (!trimmedParentName) {
      setStep(1);
      setError("ต้องระบุชื่อหมวดหมู่");
      return;
    }
    if (rows.some((row) => !row.name.trim())) {
      setError("ต้องระบุชื่อหมวดหมู่ย่อยให้ครบทุกช่อง");
      return;
    }

    setSaving(true);
    setError(null);
    try {
      let parent = persistedParent;
      if (parent) {
        if (parent.name !== trimmedParentName) {
          const result = await api.updateKnowledgeCategory(parent.id, {
            name: trimmedParentName,
            description: parent.description,
            sortOrder: parent.sortOrder,
          });
          parent = result.category;
          setPersistedParent(parent);
        }
      } else {
        const nextSortOrder =
          Math.max(0, ...categories.filter((category) => category.level === 1).map((category) => category.sortOrder)) + 1;
        const result = await api.createKnowledgeCategory({
          name: trimmedParentName,
          sortOrder: nextSortOrder,
        });
        parent = result.category;
        setPersistedParent(parent);
      }

      const nextRows = [...rows];
      for (let index = 0; index < nextRows.length; index += 1) {
        const row = nextRows[index];
        const name = row.name.trim();
        if (row.category) {
          if (row.category.name !== name) {
            const result = await api.updateKnowledgeCategory(row.category.id, {
              name,
              description: row.category.description,
              sortOrder: row.category.sortOrder,
            });
            nextRows[index] = { ...row, category: result.category, name: result.category.name };
            setRows([...nextRows]);
          }
        } else {
          const result = await api.createKnowledgeCategory({
            parentId: parent.id,
            name,
            sortOrder: index,
          });
          nextRows[index] = { key: result.category.id, category: result.category, name: result.category.name };
          setRows([...nextRows]);
        }
      }

      setRows(nextRows);
      toast.add({ title: isEditing ? "แก้ไขหมวดหมู่สำเร็จ" : "สร้างหมวดหมู่สำเร็จ", type: "success" });
      onClose();
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.response.error.message : "บันทึกหมวดหมู่ไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  const actionLabel = isEditing ? "แก้ไข" : "สร้าง";
  const title = step === 1 ? `${actionLabel}หมวดหมู่หลัก` : `${actionLabel}หมวดหมู่ย่อย`;

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !nextOpen && !saving && onClose()}>
      <DialogContent className="max-w-lg" data-testid="category-form-dialog">
        <DialogHeader className="items-center">
          <DialogTitle className="text-center text-xl font-semibold">{title}</DialogTitle>
        </DialogHeader>

        <div className="flex gap-2 items-center justify-center w-full" aria-label="ขั้นตอนจัดการหมวดหมู่">
          <StepBadge
            stepNumber={1}
            label={`${actionLabel}หมวดหมู่หลัก`}
            state={step === 1 ? "active" : "done"}
          />
          <StepBadge
            stepNumber={2}
            label={`${actionLabel}หมวดหมู่ย่อย`}
            state={step === 2 ? "active" : "upcoming"}
          />
        </div>

        {step === 1 ? (
          <Field data-invalid={Boolean(error)}>
            <FieldLabel htmlFor="category-parent-name">ชื่อหมวดหมู่</FieldLabel>
            <Input
              id="category-parent-name"
              value={parentName}
              disabled={saving}
              aria-invalid={Boolean(error)}
              onChange={(event) => {
                setParentName(event.target.value);
                setError(null);
              }}
              data-testid="category-form-parent-name-input"
            />
            <FieldError>{error}</FieldError>
          </Field>
        ) : (
          <>
            <Field data-invalid={Boolean(error)}>
              <FieldLabel htmlFor="category-parent-name">ชื่อหมวดหมู่</FieldLabel>
              <Input
                id="category-parent-name"
                value={parentName}
                disabled={saving}
                aria-invalid={Boolean(error)}
                onChange={(event) => {
                  setParentName(event.target.value);
                  setError(null);
                }}
              />
            </Field>

            <div className="border-t border-border" />

            <FieldGroup>
              {rows.map((row, index) => (
                <Field key={row.key} data-invalid={!row.name.trim() && Boolean(error)}>
                  <FieldLabel htmlFor={`subcategory-${row.key}`}>ชื่อหมวดหมู่ย่อย</FieldLabel>
                  <div className="flex items-center gap-2">
                    <Input
                      id={`subcategory-${row.key}`}
                      value={row.name}
                      disabled={saving}
                      aria-invalid={!row.name.trim() && Boolean(error)}
                      onChange={(event) => updateRow(row.key, event.target.value)}
                      data-testid={`category-form-subcategory-row-${row.key}-input`}
                    />
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      disabled={saving}
                      aria-label={`ลบช่องหมวดหมู่ย่อยที่ ${index + 1}`}
                      onClick={() => requestRemoveRow(row)}
                      data-testid={`category-form-subcategory-row-${row.key}-delete-button`}
                    >
                      <Trash2Icon />
                    </Button>
                  </div>
                </Field>
              ))}
              <Button
                type="button"
                variant="link"
                className="w-fit"
                disabled={saving}
                onClick={() => setRows((current) => [...current, createEmptyRow()])}
                data-testid="category-form-add-subcategory-button"
              >
                <PlusIcon data-icon="inline-start" />
                เพิ่มหมวดหมู่ย่อย
              </Button>
              <FieldError>{error}</FieldError>
            </FieldGroup>
          </>
        )}

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            disabled={saving}
            onClick={onClose}
            data-testid="category-form-cancel-button"
          >
            ยกเลิก
          </Button>
          {step === 1 ? (
            <Button type="button" onClick={handleNext} data-testid="category-form-next-button">
              ถัดไป
            </Button>
          ) : (
            <Button
              type="button"
              disabled={saving}
              onClick={() => void handleConfirm()}
              data-testid="category-form-confirm-button"
            >
              {saving && <Spinner data-icon="inline-start" />}
              ยืนยัน
            </Button>
          )}
        </DialogFooter>

        {/* CD-5 point 1 - replaces window.confirm for deleting a subcategory row. CD-7: a nested
            dialog inside CategoryFormDialog's own Dialog - cancelling or confirming must return to
            this same form without resetting it or closing the parent dialog. */}
        <AlertDialog open={pendingRemoval !== null} onOpenChange={(next) => !next && setPendingRemoval(null)}>
          <AlertDialogContent data-testid="category-form-remove-subcategory-dialog">
            <AlertDialogHeader>
              <AlertDialogTitle>ลบหมวดหมู่ย่อย</AlertDialogTitle>
              <AlertDialogDescription>
                ต้องการลบหมวดหมู่ย่อย &quot;{pendingRemoval?.category?.name}&quot; ใช่หรือไม่?
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel data-testid="category-form-remove-subcategory-cancel-button">
                ยกเลิก
              </AlertDialogCancel>
              <AlertDialogAction
                variant="destructive"
                onClick={() => void confirmRemoveRow()}
                data-testid="category-form-remove-subcategory-confirm-button"
              >
                ลบหมวดหมู่ย่อย
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </DialogContent>
    </Dialog>
  );
}

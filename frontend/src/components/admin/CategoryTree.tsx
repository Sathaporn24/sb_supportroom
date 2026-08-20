"use client";

import { LockIcon, PencilIcon, PlusIcon, Trash2Icon } from "lucide-react";
import type { KnowledgeCategory } from "@/types/domain";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Skeleton } from "@/components/ui/skeleton";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

const SKELETON_PARENT_COUNT = 3;
const SKELETON_CHILD_COUNT = 2;

/** Mirrors CategoryTree's own shape (a Card per Level 1 category with Level 2 rows inside) so the
 * loading state doesn't jump in layout once the real tree renders. */
export function CategoryTreeSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      {Array.from({ length: SKELETON_PARENT_COUNT }).map((_, parentIndex) => (
        <Card key={parentIndex}>
          <CardHeader className="flex flex-row items-center justify-between gap-2">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-8 w-20" />
          </CardHeader>
          <CardContent className="flex flex-col gap-1">
            <ul className="flex flex-col divide-y divide-border rounded-lg border">
              {Array.from({ length: SKELETON_CHILD_COUNT }).map((_, childIndex) => (
                <li key={childIndex} className="flex items-center justify-between gap-2 px-3 py-2">
                  <Skeleton className="h-4 w-40" />
                  <Skeleton className="h-6 w-14" />
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

type Props = {
  categories: KnowledgeCategory[];
  onAddChild: (parent: KnowledgeCategory) => void;
  onEdit: (category: KnowledgeCategory) => void;
  onDelete: (category: KnowledgeCategory) => void;
};

const SYSTEM_DEFAULT_TOOLTIP =
  "หมวดเริ่มต้นของระบบ - ที่เก็บบทเรียนที่ยังไม่ได้จัดหมวด แก้ไขหรือลบไม่ได้";

/** Renders the 2-level taxonomy (design.md DM-1): a Card per Level 1 category with its
 * Level 2 subcategories listed inside. isSystemDefault rows always render (never hidden) but
 * with edit/delete disabled - TX-11 blocks those server-side too, this just avoids a round trip. */
export function CategoryTree({ categories, onAddChild, onEdit, onDelete }: Props) {
  const parents = categories
    .filter((c) => c.level === 1)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));

  if (parents.length === 0) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>ยังไม่มีหมวด</EmptyTitle>
          <EmptyDescription>เริ่มด้วยการเพิ่มหมวดใหญ่ก่อนค่ะ</EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {parents.map((parent) => {
        const children = categories
          .filter((c) => c.level === 2 && c.parentId === parent.id)
          .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));

        return (
          <Card key={parent.id}>
            <CardHeader className="flex flex-row items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <CardTitle className="text-sm font-semibold">{parent.name}</CardTitle>
                {parent.isSystemDefault && <SystemDefaultBadge />}
              </div>
              <div className="flex items-center gap-1">
                <Button variant="ghost" size="sm" onClick={() => onAddChild(parent)}>
                  <PlusIcon data-icon="inline-start" />
                  หมวดย่อย
                </Button>
                <CategoryActions category={parent} onEdit={onEdit} onDelete={onDelete} />
              </div>
            </CardHeader>
            <CardContent className="flex flex-col gap-1">
              {parent.description && <p className="text-xs text-muted-foreground">{parent.description}</p>}
              {children.length === 0 ? (
                <p className="text-xs text-muted-foreground">ยังไม่มีหมวดย่อย</p>
              ) : (
                <ul className="flex flex-col divide-y divide-border rounded-lg border">
                  {children.map((child) => (
                    <li key={child.id} className="flex items-center justify-between gap-2 px-3 py-2">
                      <div className="flex min-w-0 items-center gap-2">
                        <span className="truncate text-sm">{child.name}</span>
                        {child.isSystemDefault && <SystemDefaultBadge />}
                      </div>
                      <CategoryActions category={child} onEdit={onEdit} onDelete={onDelete} />
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}

function SystemDefaultBadge() {
  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <Badge variant="secondary" className="gap-1">
            <LockIcon data-icon="inline-start" />
            ค่าเริ่มต้นของระบบ
          </Badge>
        }
      />
      <TooltipContent>{SYSTEM_DEFAULT_TOOLTIP}</TooltipContent>
    </Tooltip>
  );
}

function CategoryActions({
  category,
  onEdit,
  onDelete,
}: {
  category: KnowledgeCategory;
  onEdit: (category: KnowledgeCategory) => void;
  onDelete: (category: KnowledgeCategory) => void;
}) {
  if (category.isSystemDefault) {
    return (
      <Tooltip>
        <TooltipTrigger
          render={
            <span className="flex items-center gap-1">
              <Button variant="ghost" size="icon-sm" disabled>
                <PencilIcon />
              </Button>
              <Button variant="ghost" size="icon-sm" disabled>
                <Trash2Icon />
              </Button>
            </span>
          }
        />
        <TooltipContent>{SYSTEM_DEFAULT_TOOLTIP}</TooltipContent>
      </Tooltip>
    );
  }

  return (
    <div className="flex items-center gap-1">
      <Button variant="ghost" size="icon-sm" onClick={() => onEdit(category)}>
        <PencilIcon />
      </Button>
      <Button variant="ghost" size="icon-sm" onClick={() => onDelete(category)}>
        <Trash2Icon />
      </Button>
    </div>
  );
}

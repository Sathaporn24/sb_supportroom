"use client";

import { LinkIcon, LockIcon, PencilIcon, PlusIcon, Trash2Icon } from "lucide-react";
import { AdminLink } from "@/components/admin/AdminLink";
import type { KnowledgeCategory, LessonConfig } from "@/types/domain";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

const SKELETON_PARENT_COUNT = 3;
const SKELETON_CHILD_COUNT = 2;
const SYSTEM_DEFAULT_TOOLTIP =
  "หมวดเริ่มต้นของระบบ - ที่เก็บบทเรียนที่ยังไม่ได้จัดหมวด แก้ไขหรือลบไม่ได้";

export function CategoryTreeSkeleton() {
  return (
    <div className="flex flex-col gap-[49px]">
      {Array.from({ length: SKELETON_PARENT_COUNT }).map((_, parentIndex) => (
        <div key={parentIndex} className="flex flex-col gap-4 border-b pb-4">
          <div className="flex flex-row items-center justify-between gap-2">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-8 w-28" />
          </div>
          <div className="flex flex-col gap-3">
            {Array.from({ length: SKELETON_CHILD_COUNT }).map((_, childIndex) => (
              <Skeleton key={childIndex} className="h-10 w-full" />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

type Props = {
  categories: KnowledgeCategory[];
  lessons: LessonConfig[];
  busyLessonId: string | null;
  onEditParent: (category: KnowledgeCategory) => void;
  onDeleteParent: (category: KnowledgeCategory) => void;
  onToggleLesson: (lesson: LessonConfig, checked: boolean) => void;
  onCreateLink: (lesson: LessonConfig) => void;
};

export function CategoryTree({
  categories,
  lessons,
  busyLessonId,
  onEditParent,
  onDeleteParent,
  onToggleLesson,
  onCreateLink,
}: Props) {
  const parents = categories
    .filter((category) => category.level === 1)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));

  if (parents.length === 0) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>ยังไม่มีหมวดหมู่</EmptyTitle>
          <EmptyDescription>เริ่มด้วยการสร้างหมวดหมู่หลักก่อนค่ะ</EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <div className="flex flex-col gap-[49px]">
      {parents.map((parent) => {
        const children = categories
          .filter((category) => category.level === 2 && category.parentId === parent.id)
          .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));

        return (
          <div key={parent.id} className="flex flex-col gap-4">
            <div className="flex flex-row flex-wrap items-center justify-between gap-3">
              <div className="flex min-w-0 items-center gap-2">
                <p className="font-heading truncate text-base leading-snug font-medium">{parent.name}</p>
                {parent.isSystemDefault && <SystemDefaultBadge />}
              </div>
              <div className="flex items-center gap-1">
                <AdminLink
                  href="/admin/lessons/new"
                  className={cn(
                    buttonVariants({ size: "sm" }),
                    "bg-info text-info-foreground hover:bg-info/90",
                  )}
                  data-testid={`category-tree-add-lesson-link-${parent.id}`}
                >
                  <PlusIcon data-icon="inline-start" />
                  เพิ่มบทเรียน
                </AdminLink>
                <CategoryActions
                  category={parent}
                  onEdit={onEditParent}
                  onDelete={onDeleteParent}
                />
              </div>
            </div>
            {children.length === 0 ? (
              <Empty className="border">
                <EmptyHeader>
                  <EmptyTitle>ยังไม่มีหมวดหมู่ย่อย</EmptyTitle>
                  <EmptyDescription>กดแก้ไขหมวดหมู่หลักเพื่อเพิ่มหมวดหมู่ย่อย</EmptyDescription>
                </EmptyHeader>
              </Empty>
            ) : (
              <Accordion multiple defaultValue={[children[0].id]}>
                {children.map((child) => {
                  const childLessons = lessons
                    .filter((lesson) => lesson.categoryId === child.id)
                    .sort((a, b) => a.title.localeCompare(b.title, "th"));

                  return (
                    <AccordionItem key={child.id} value={child.id} className="border-b">
                      <AccordionTrigger
                        className="min-h-[60px] items-center rounded-none border-0 px-6 py-3.5 text-base font-semibold aria-expanded:bg-tree-subcategory aria-expanded:text-tree-subcategory-foreground"
                        data-testid={`category-subrow-${child.id}-trigger`}
                      >
                        <span className="flex min-w-0 items-center gap-2">
                          <span className="truncate">{child.name}</span>
                          {child.isSystemDefault && <SystemDefaultBadge />}
                        </span>
                      </AccordionTrigger>
                      <AccordionContent className="bg-card p-0">
                        <LessonTable
                          lessons={childLessons}
                          busyLessonId={busyLessonId}
                          onToggleLesson={onToggleLesson}
                          onCreateLink={onCreateLink}
                        />
                      </AccordionContent>
                    </AccordionItem>
                  );
                })}
              </Accordion>
            )}
          </div>
        );
      })}
    </div>
  );
}

function LessonTable({
  lessons,
  busyLessonId,
  onToggleLesson,
  onCreateLink,
}: {
  lessons: LessonConfig[];
  busyLessonId: string | null;
  onToggleLesson: (lesson: LessonConfig, checked: boolean) => void;
  onCreateLink: (lesson: LessonConfig) => void;
}) {
  return (
    <Table>
      <TableHeader>
        <TableRow className="border-b-0 bg-tree-header hover:bg-tree-header">
          <TableHead className="h-[60px] px-6 text-base font-semibold text-foreground">ชื่อบทเรียน</TableHead>
          <TableHead className="h-[60px] w-[240px] px-6 text-base font-semibold text-foreground">สถานะ</TableHead>
          <TableHead className="h-[60px] w-[296px] px-6 text-base font-semibold text-foreground">จัดการ</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {lessons.length === 0 ? (
          <TableRow className="border-b-0 bg-background hover:bg-background">
            <TableCell colSpan={3} className="px-6 py-6 text-center text-muted-foreground">
              ยังไม่มีบทเรียนในหมวดหมู่ย่อยนี้
            </TableCell>
          </TableRow>
        ) : (
          lessons.map((lesson) => (
            <TableRow
              key={lesson.id}
              className="border-b-0 bg-background hover:bg-background"
              data-testid={`lesson-row-${lesson.slug}`}
            >
              <TableCell className="h-[60px] px-6 font-medium">{lesson.title}</TableCell>
              <TableCell className="h-[60px] w-[240px] px-6">
                <div className="flex items-center gap-2">
                  <Switch
                    checked={lesson.isActive}
                    disabled={busyLessonId !== null}
                    aria-label={`${lesson.isActive ? "ปิด" : "เปิด"}ใช้งานบทเรียน ${lesson.title}`}
                    onCheckedChange={(checked) => onToggleLesson(lesson, checked)}
                    data-testid={`lesson-row-${lesson.slug}-active-switch`}
                  />
                  <span className="text-xs text-muted-foreground">
                    {lesson.isActive ? "เปิดใช้งาน" : "ปิดใช้งาน"}
                  </span>
                </div>
              </TableCell>
              <TableCell className="h-[60px] w-[296px] px-6">
                <div className="flex items-center gap-2.5">
                  <Button
                    type="button"
                    size="sm"
                    disabled={!lesson.isActive}
                    onClick={() => onCreateLink(lesson)}
                    data-testid={`lesson-row-${lesson.slug}-create-link-button`}
                  >
                    <LinkIcon data-icon="inline-start" />
                    สร้างลิงก์การสอน
                  </Button>
                  <AdminLink
                    href={`/admin/lessons/${encodeURIComponent(lesson.slug)}`}
                    aria-label={`แก้ไขบทเรียน ${lesson.title}`}
                    className={buttonVariants({ variant: "outline", size: "icon-sm" })}
                    data-testid={`lesson-row-${lesson.slug}-edit-link`}
                  >
                    <PencilIcon />
                  </AdminLink>
                </div>
              </TableCell>
            </TableRow>
          ))
        )}
      </TableBody>
    </Table>
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
              <Button
                variant="outline"
                size="icon-sm"
                disabled
                aria-label="แก้ไขหมวดหมู่"
                data-testid={`category-row-${category.id}-edit-button`}
              >
                <PencilIcon />
              </Button>
              <Button
                variant="outline"
                size="icon-sm"
                disabled
                aria-label="ลบหมวดหมู่"
                data-testid={`category-row-${category.id}-delete-button`}
              >
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
      <Button
        type="button"
        variant="outline"
        size="icon-sm"
        aria-label={`แก้ไขหมวดหมู่ ${category.name}`}
        onClick={() => onEdit(category)}
        data-testid={`category-row-${category.id}-edit-button`}
      >
        <PencilIcon />
      </Button>
      <Button
        type="button"
        variant="outline"
        size="icon-sm"
        aria-label={`ลบหมวดหมู่ ${category.name}`}
        onClick={() => onDelete(category)}
        data-testid={`category-row-${category.id}-delete-button`}
      >
        <Trash2Icon />
      </Button>
    </div>
  );
}

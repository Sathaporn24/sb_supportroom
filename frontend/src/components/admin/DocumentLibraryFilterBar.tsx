"use client";

import { useMemo } from "react";
import type {
  DocumentIndexingStatus,
  KnowledgeCategory,
  KnowledgeQnAFilter,
  KnowledgeScopeType,
  LessonConfig,
} from "@/types/domain";
import { statusLabels } from "@/components/admin/DocumentUploadList";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const INDEXING_STATUS_VALUES: DocumentIndexingStatus[] = ["pending", "indexed", "failed"];

/** KL-12 - below this the server treats `q` as "not searching", not an error; the frontend mirrors
 * that so the note under the input and the actual filtering behaviour never disagree. */
export const MIN_CONTENT_SEARCH_LENGTH = 2;

type Props = {
  filter: KnowledgeQnAFilter;
  onFilterChange: (next: KnowledgeQnAFilter) => void;
  searchInput: string;
  onSearchInputChange: (next: string) => void;
  categories: KnowledgeCategory[];
  lessons: LessonConfig[];
};

function scopeSelectValue(filter: KnowledgeQnAFilter): string {
  if (!filter.scopeType) return "all";
  if (filter.scopeType === "company") return "company";
  return `${filter.scopeType}:${filter.scopeId ?? ""}`;
}

/**
 * KL-1/KL-4 - one filter+search bar drives both the documents table and the Q&A table below it on
 * `/admin/documents`; neither table keeps its own copy of scope/search state. KL-4 - four scope
 * options ("ทั้งหมด" default per KL-2, "ทั้งบริษัท", "เฉพาะหมวด", "เฉพาะบทเรียน" - the last one new
 * this phase, using the same `listLessons()` data the badge/scope-label logic already loads).
 */
export function DocumentLibraryFilterBar({
  filter,
  onFilterChange,
  searchInput,
  onSearchInputChange,
  categories,
  lessons,
}: Props) {
  const subcategories = useMemo(
    () =>
      categories
        .filter((c) => c.level === 2)
        .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th")),
    [categories],
  );
  const parentsById = useMemo(
    () => new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c])),
    [categories],
  );
  const sortedLessons = useMemo(() => [...lessons].sort((a, b) => a.title.localeCompare(b.title, "th")), [lessons]);

  const selectValue = scopeSelectValue(filter);

  function handleScopeSelect(value: string | null) {
    if (!value || value === "all") {
      onFilterChange({ ...filter, scopeType: undefined, scopeId: undefined });
      return;
    }
    if (value === "company") {
      onFilterChange({ ...filter, scopeType: "company", scopeId: undefined });
      return;
    }
    const separatorIndex = value.indexOf(":");
    const scopeType = value.slice(0, separatorIndex) as KnowledgeScopeType;
    const scopeId = value.slice(separatorIndex + 1);
    if (!scopeId) return;
    onFilterChange({ ...filter, scopeType, scopeId });
  }

  function handleStatusSelect(value: string | null) {
    if (!value || value === "all") {
      onFilterChange({ ...filter, status: undefined });
      return;
    }
    onFilterChange({ ...filter, status: value as DocumentIndexingStatus });
  }

  return (
    <div className="flex flex-col gap-3 rounded-xl border p-3">
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex flex-col gap-2">
          <Label>ขอบเขต</Label>
          <Select value={selectValue} onValueChange={handleScopeSelect}>
            <SelectTrigger className="w-64" data-testid="documents-filter-scope-select">
              <SelectValue placeholder="เลือกขอบเขต">
                {(value: string) => {
                  if (value === "all") return "ทั้งหมด";
                  if (value === "company") return "ทั้งบริษัท";
                  if (value.startsWith("category:")) {
                    const sub = subcategories.find((c) => c.id === value.slice("category:".length));
                    return sub ? `${parentsById.get(sub.parentId ?? "")?.name ?? "?"} › ${sub.name}` : "เฉพาะหมวด";
                  }
                  if (value.startsWith("lesson:")) {
                    const lesson = sortedLessons.find((l) => l.id === value.slice("lesson:".length));
                    return lesson ? lesson.title : "เฉพาะบทเรียน";
                  }
                  return "เลือกขอบเขต";
                }}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">ทั้งหมด</SelectItem>
              <SelectItem value="company">ทั้งบริษัท</SelectItem>
              <SelectGroup>
                <SelectLabel>เฉพาะหมวด</SelectLabel>
                {subcategories.map((sub) => (
                  <SelectItem key={sub.id} value={`category:${sub.id}`}>
                    {parentsById.get(sub.parentId ?? "")?.name ?? "?"} › {sub.name}
                  </SelectItem>
                ))}
              </SelectGroup>
              <SelectGroup>
                <SelectLabel>เฉพาะบทเรียน</SelectLabel>
                {sortedLessons.map((lesson) => (
                  <SelectItem key={lesson.id} value={`lesson:${lesson.id}`}>
                    {lesson.title}
                  </SelectItem>
                ))}
              </SelectGroup>
            </SelectContent>
          </Select>
        </div>

        <div className="flex min-w-60 flex-1 flex-col gap-2">
          <Label htmlFor="documents-search-input">ค้นในเนื้อหา</Label>
          <Input
            id="documents-search-input"
            data-testid="documents-search-input"
            value={searchInput}
            onChange={(e) => onSearchInputChange(e.target.value)}
            placeholder="พิมพ์คำที่ต้องการค้นหา..."
          />
        </div>

        <div className="flex flex-col gap-2">
          <Label>สถานะ</Label>
          <Select value={filter.status ?? "all"} onValueChange={handleStatusSelect}>
            <SelectTrigger className="w-48" data-testid="documents-filter-status-select">
              <SelectValue placeholder="เลือกสถานะ">
                {(value: string) => (value === "all" ? "ทั้งหมด" : statusLabels[value as DocumentIndexingStatus])}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">ทั้งหมด</SelectItem>
              {INDEXING_STATUS_VALUES.map((status) => (
                <SelectItem key={status} value={status}>
                  {statusLabels[status]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
      {/* KL-12/KL-13 - both notes are contract requirements, not incidental copy: CS must not
          conclude a file "disappeared" from the library just because it failed to index. */}
      <p className="text-xs text-muted-foreground">
        ต้องพิมพ์อย่างน้อย {MIN_CONTENT_SEARCH_LENGTH} ตัวอักษรจึงจะเริ่มค้นหา · การค้นเนื้อหาไม่ครอบเอกสารที่ index
        ไม่สำเร็จ (ค้นได้จากชื่อไฟล์เท่านั้น)
      </p>
    </div>
  );
}

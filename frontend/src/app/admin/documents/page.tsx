"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import * as api from "@/lib/api-client";
import type { KnowledgeCategory, KnowledgeQnAFilter, KnowledgeScopeType, LessonConfig } from "@/types/domain";
import { DeletedDocumentsList } from "@/components/admin/DeletedDocumentsList";
import { DocumentLibraryFilterBar, MIN_CONTENT_SEARCH_LENGTH } from "@/components/admin/DocumentLibraryFilterBar";
import { DocumentUploadList } from "@/components/admin/DocumentUploadList";
import { KnowledgeQnATable } from "@/components/admin/KnowledgeQnATable";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

const KNOWN_SCOPE_TYPES: KnowledgeScopeType[] = ["lesson", "category", "company"];

/** Debounces the search box so every keystroke doesn't fire a fresh request to both tables. */
const SEARCH_DEBOUNCE_MS = 300;

/** KL-2 - "ทั้งหมด" (every scope) is the default whenever the query string is missing or
 * unrecognized - not "company" like the old single-scope endpoint used to assume. */
function initialFilterFromSearchParams(searchParams: URLSearchParams): KnowledgeQnAFilter {
  const scopeType = searchParams.get("scopeType");
  const scopeId = searchParams.get("scopeId") ?? undefined;
  if (scopeType && KNOWN_SCOPE_TYPES.includes(scopeType as KnowledgeScopeType)) {
    if (scopeType === "company") {
      return { scopeType: "company" };
    }
    if (scopeId) {
      return { scopeType: scopeType as KnowledgeScopeType, scopeId };
    }
  }
  return {};
}

export default function DocumentsLibraryPage() {
  const searchParams = useSearchParams();
  const [filter, setFilter] = useState<KnowledgeQnAFilter>(() => initialFilterFromSearchParams(searchParams));
  const [searchInput, setSearchInput] = useState("");
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [lessons, setLessons] = useState<LessonConfig[]>([]);

  useEffect(() => {
    void Promise.all([api.listKnowledgeCategories(), api.listLessons()]).then(
      ([{ categories: categoryList }, { lessons: lessonList }]) => {
        setCategories(categoryList);
        setLessons(lessonList);
      },
    );
  }, []);

  // KL-12 - under MIN_CONTENT_SEARCH_LENGTH characters is "not searching", not an error: the
  // debounced query below is simply omitted rather than sent and rejected.
  useEffect(() => {
    const trimmed = searchInput.trim();
    const timer = setTimeout(() => {
      setFilter((prev) => ({
        ...prev,
        q: trimmed.length >= MIN_CONTENT_SEARCH_LENGTH ? trimmed : undefined,
      }));
    }, SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchInput]);

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-primary">คลังความรู้</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          อัปโหลดเอกสารและจัดการคำถาม-คำตอบ ใช้ตอบได้ทั้งบริษัท เฉพาะหมวด หรือเฉพาะบทเรียนก็ได้
        </p>
      </div>

      <Tabs defaultValue="active">
        <TabsList>
          <TabsTrigger value="active" data-testid="documents-active-tab">
            คลังความรู้
          </TabsTrigger>
          <TabsTrigger value="deleted" data-testid="documents-deleted-tab">
            กู้คืนเอกสารที่ถูกลบ
          </TabsTrigger>
        </TabsList>
        <TabsContent value="active" className="flex flex-col gap-6">
          <DocumentLibraryFilterBar
            filter={filter}
            onFilterChange={setFilter}
            searchInput={searchInput}
            onSearchInputChange={setSearchInput}
            categories={categories}
            lessons={lessons}
          />

          <section className="flex flex-col gap-3">
            <h2 className="text-base font-semibold">เอกสาร</h2>
            <DocumentUploadList filter={filter} categories={categories} lessons={lessons} />
          </section>

          <section className="flex flex-col gap-3">
            <h2 className="text-base font-semibold">คำถาม-คำตอบ (Q&amp;A)</h2>
            <KnowledgeQnATable filter={filter} categories={categories} lessons={lessons} />
          </section>
        </TabsContent>
        <TabsContent value="deleted">
          <DeletedDocumentsList />
        </TabsContent>
      </Tabs>
    </main>
  );
}

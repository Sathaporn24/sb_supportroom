"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { PlusIcon } from "lucide-react";
import { CategoryFormDialog } from "@/components/admin/CategoryFormDialog";
import { CategoryTree, CategoryTreeSkeleton } from "@/components/admin/CategoryTree";
import { CreateTrainingLinkModal } from "@/components/admin/CreateTrainingLinkModal";
import { OnboardingChecklist } from "@/components/admin/OnboardingChecklist";
import { TrainingLinksTable } from "@/components/admin/TrainingLinksTable";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { TableSkeleton } from "@/components/shared/TableSkeleton";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory, LessonConfig, LessonConfigInput, TrainingLink } from "@/types/domain";

type LessonTab = "manage" | "links";

function toLessonInput(lesson: LessonConfig, isActive: boolean): LessonConfigInput {
  return {
    slug: lesson.slug,
    categoryId: lesson.categoryId,
    title: lesson.title,
    description: lesson.description,
    slidesSourceUrl: lesson.slidesSourceUrl,
    slidesEmbedUrl: lesson.slidesEmbedUrl,
    contentSourceType: lesson.contentSourceType,
    pdfDocumentResourceId: lesson.pdfDocumentResourceId,
    slideConfigs: lesson.slideConfigs,
    isActive,
  };
}

export default function LessonsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { user, activeCompanyId } = useAdminSession();
  const companyId = activeCompanyId ?? user?.companyId ?? null;
  const activeTab: LessonTab = searchParams.get("tab") === "links" ? "links" : "manage";

  const [lessons, setLessons] = useState<LessonConfig[] | null>(null);
  const [categories, setCategories] = useState<KnowledgeCategory[] | null>(null);
  const [links, setLinks] = useState<TrainingLink[] | null>(null);
  const [origin, setOrigin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busyLessonId, setBusyLessonId] = useState<string | null>(null);
  const [categoryDialogOpen, setCategoryDialogOpen] = useState(false);
  const [editingParent, setEditingParent] = useState<KnowledgeCategory | null>(null);
  const [selectedLesson, setSelectedLesson] = useState<LessonConfig | null>(null);
  const [resetting, setResetting] = useState(false);

  const reload = useCallback(async () => {
    if (!companyId) return;
    setError(null);
    try {
      const [lessonResult, categoryResult, linkResult] = await Promise.all([
        api.listLessons(),
        api.listKnowledgeCategories(),
        api.listTrainingLinks(),
      ]);
      setLessons(lessonResult.lessons);
      setCategories(categoryResult.categories);
      setLinks(linkResult.links);
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.response.error.message : "โหลดข้อมูลบทเรียนไม่สำเร็จ");
    }
  }, [companyId]);

  useEffect(() => {
    setOrigin(window.location.origin);
    void reload();
  }, [reload]);

  function handleTabChange(value: string) {
    const nextTab: LessonTab = value === "links" ? "links" : "manage";
    const params = new URLSearchParams(searchParams.toString());
    if (nextTab === "manage") params.delete("tab");
    else params.set("tab", nextTab);
    const query = params.toString();
    router.replace(query ? `/admin/lessons?${query}` : "/admin/lessons", { scroll: false });
  }

  async function handleResetDemoData() {
    const confirmed = window.confirm("ต้องการรีเซ็ตข้อมูล Demo ทั้งหมดกลับเป็นค่าเริ่มต้นใช่หรือไม่?");
    if (!confirmed) return;
    setResetting(true);
    try {
      await api.resetDemoData();
      await reload();
    } finally {
      setResetting(false);
    }
  }

  function openCreateCategoryDialog() {
    setEditingParent(null);
    setCategoryDialogOpen(true);
  }

  function openEditCategoryDialog(parent: KnowledgeCategory) {
    setEditingParent(parent);
    setCategoryDialogOpen(true);
  }

  async function handleDeleteParent(parent: KnowledgeCategory) {
    const confirmed = window.confirm(`ต้องการลบหมวดหมู่ "${parent.name}" ใช่หรือไม่?`);
    if (!confirmed) return;
    setError(null);
    try {
      await api.deleteKnowledgeCategory(parent.id);
      setCategories((current) => current?.filter((category) => category.id !== parent.id) ?? current);
    } catch (caught) {
      // TX-6 explains exactly which lessons/documents/Q&A/subcategories still block deletion.
      setError(caught instanceof ApiClientError ? caught.response.error.message : "ลบหมวดหมู่ไม่สำเร็จ");
    }
  }

  async function handleToggleLesson(lesson: LessonConfig, checked: boolean) {
    setBusyLessonId(lesson.id);
    setError(null);
    try {
      const { lesson: saved } = await api.saveLesson(toLessonInput(lesson, checked));
      setLessons((current) => current?.map((item) => (item.id === saved.id ? saved : item)) ?? current);
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.response.error.message : "บันทึกสถานะบทเรียนไม่สำเร็จ");
    } finally {
      setBusyLessonId(null);
    }
  }

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold text-primary">บทเรียน</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            จัดการเนื้อหาบทเรียน และสร้างลิงก์กับบทเรียน
          </p>
        </div>
        <div className="flex items-center gap-2">
          {user?.role === "owner" && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => void handleResetDemoData()}
              disabled={resetting}
              data-testid="lessons-reset-demo-button"
            >
              {resetting ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังรีเซ็ต...
                </>
              ) : (
                "Reset Demo Data"
              )}
            </Button>
          )}
          {activeTab === "manage" && (
            <Button type="button" onClick={openCreateCategoryDialog} data-testid="lessons-create-category-button">
              <PlusIcon data-icon="inline-start" />
              สร้างหมวดหมู่
            </Button>
          )}
        </div>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!companyId ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ยังไม่มีบริษัทที่กำลังดูอยู่</EmptyTitle>
            <EmptyDescription>เลือกบริษัทก่อนจัดการบทเรียน</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Tabs value={activeTab} onValueChange={handleTabChange}>
          <TabsList>
            <TabsTrigger value="manage" data-testid="lessons-tab-manage">จัดการบทเรียน</TabsTrigger>
            <TabsTrigger value="links" data-testid="lessons-tab-links">ประวัติการสร้างลิงก์</TabsTrigger>
          </TabsList>

          <TabsContent value="manage" className="pt-4">
            {!lessons || !categories ? (
              <CategoryTreeSkeleton />
            ) : (
              <CategoryTree
                categories={categories}
                lessons={lessons}
                busyLessonId={busyLessonId}
                onEditParent={openEditCategoryDialog}
                onDeleteParent={(parent) => void handleDeleteParent(parent)}
                onToggleLesson={(lesson, checked) => void handleToggleLesson(lesson, checked)}
                onCreateLink={setSelectedLesson}
              />
            )}
          </TabsContent>

          <TabsContent value="links" className="pt-4">
            {!links ? (
              <TableSkeleton columns={6} />
            ) : links.length === 0 ? (
              <OnboardingChecklist hasLesson={(lessons?.length ?? 0) > 0} />
            ) : (
              <TrainingLinksTable links={links} origin={origin} />
            )}
          </TabsContent>
        </Tabs>
      )}

      <CategoryFormDialog
        open={categoryDialogOpen}
        categories={categories ?? []}
        editingParent={editingParent}
        onClose={() => {
          setCategoryDialogOpen(false);
          void reload();
        }}
      />

      <CreateTrainingLinkModal
        open={selectedLesson !== null}
        lesson={selectedLesson}
        onClose={() => {
          setSelectedLesson(null);
          void reload();
        }}
      />
    </main>
  );
}

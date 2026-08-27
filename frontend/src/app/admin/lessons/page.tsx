"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { PlusIcon } from "lucide-react";
import { CategoryFormDialog } from "@/components/admin/CategoryFormDialog";
import { CategoryTree, CategoryTreeSkeleton } from "@/components/admin/CategoryTree";
import { CreateTrainingLinkModal } from "@/components/admin/CreateTrainingLinkModal";
import { LessonTrashList } from "@/components/admin/LessonTrashList";
import { OnboardingChecklist } from "@/components/admin/OnboardingChecklist";
import { TrainingLinksTable } from "@/components/admin/TrainingLinksTable";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
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
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { TableSkeleton } from "@/components/shared/TableSkeleton";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory, LessonConfig, LessonConfigInput, TrainingLink } from "@/types/domain";

type LessonTab = "manage" | "links" | "trash";

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
  const activeTab: LessonTab =
    searchParams.get("tab") === "links" ? "links" : searchParams.get("tab") === "trash" ? "trash" : "manage";

  const [lessons, setLessons] = useState<LessonConfig[] | null>(null);
  const [categories, setCategories] = useState<KnowledgeCategory[] | null>(null);
  const [links, setLinks] = useState<TrainingLink[] | null>(null);
  const [origin, setOrigin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busyLessonId, setBusyLessonId] = useState<string | null>(null);
  // LT-3/LT-4 - archiving here or restoring from the trash tab both change the other list, so
  // this is how the trash tab is told to reload without lifting its whole state up.
  const [trashRefreshToken, setTrashRefreshToken] = useState(0);
  const [categoryDialogOpen, setCategoryDialogOpen] = useState(false);
  const [editingParent, setEditingParent] = useState<KnowledgeCategory | null>(null);
  const [selectedLesson, setSelectedLesson] = useState<LessonConfig | null>(null);
  const [resetting, setResetting] = useState(false);
  // CD-2/CD-4 - yes/no confirms replacing window.confirm. `pendingResetDemoData` has no payload
  // (the action needs none); `pendingDeleteParent`/`pendingArchiveLesson` hold the row itself,
  // not a closure.
  const [pendingResetDemoData, setPendingResetDemoData] = useState(false);
  const [pendingDeleteParent, setPendingDeleteParent] = useState<KnowledgeCategory | null>(null);
  const [pendingArchiveLesson, setPendingArchiveLesson] = useState<LessonConfig | null>(null);

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
    const nextTab: LessonTab = value === "links" ? "links" : value === "trash" ? "trash" : "manage";
    const params = new URLSearchParams(searchParams.toString());
    if (nextTab === "manage") params.delete("tab");
    else params.set("tab", nextTab);
    const query = params.toString();
    router.replace(query ? `/admin/lessons?${query}` : "/admin/lessons", { scroll: false });
  }

  async function confirmResetDemoData() {
    setPendingResetDemoData(false);
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

  async function confirmDeleteParent() {
    const parent = pendingDeleteParent;
    if (!parent) return;
    setPendingDeleteParent(null);
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

  /** LT-3 - archiving revokes every TrainingLink of this lesson immediately; the confirmation
   * copy says so, since it's the one irreversible-ish part of an otherwise reversible action. */
  async function confirmArchiveLesson() {
    const lesson = pendingArchiveLesson;
    if (!lesson) return;
    setPendingArchiveLesson(null);
    setBusyLessonId(lesson.id);
    setError(null);
    try {
      await api.archiveLesson(lesson.id);
      setLessons((current) => current?.filter((item) => item.id !== lesson.id) ?? current);
      setTrashRefreshToken((token) => token + 1);
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.response.error.message : "ย้ายบทเรียนไปถังขยะไม่สำเร็จ");
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
              onClick={() => setPendingResetDemoData(true)}
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
            <TabsTrigger value="trash" data-testid="lessons-tab-trash">ถังขยะ</TabsTrigger>
          </TabsList>

          <TabsContent value="manage" className="pt-4">
            {!lessons || !categories ? (
              <CategoryTreeSkeleton />
            ) : (
              <CategoryTree
                categories={categories}
                lessons={lessons}
                busyLessonId={busyLessonId}
                role={user?.role ?? "cs"}
                onEditParent={openEditCategoryDialog}
                onDeleteParent={(parent) => setPendingDeleteParent(parent)}
                onToggleLesson={(lesson, checked) => void handleToggleLesson(lesson, checked)}
                onCreateLink={setSelectedLesson}
                onArchiveLesson={(lesson) => setPendingArchiveLesson(lesson)}
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

          <TabsContent value="trash" className="pt-4">
            <LessonTrashList
              role={user?.role ?? "cs"}
              refreshToken={trashRefreshToken}
              onLessonRestored={() => void reload()}
            />
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

      {/* CD-5 point 3 - replaces window.confirm for resetting demo data. */}
      <AlertDialog open={pendingResetDemoData} onOpenChange={setPendingResetDemoData}>
        <AlertDialogContent data-testid="lessons-reset-demo-dialog">
          <AlertDialogHeader>
            <AlertDialogTitle>รีเซ็ตข้อมูล Demo</AlertDialogTitle>
            <AlertDialogDescription>
              ต้องการรีเซ็ตข้อมูล Demo ทั้งหมดกลับเป็นค่าเริ่มต้นใช่หรือไม่?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel data-testid="lessons-reset-demo-cancel-button">ยกเลิก</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => void confirmResetDemoData()}
              data-testid="lessons-reset-demo-confirm-button"
            >
              รีเซ็ตข้อมูล
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* CD-5 point 4 - replaces window.confirm for deleting a parent category. */}
      <AlertDialog open={pendingDeleteParent !== null} onOpenChange={(next) => !next && setPendingDeleteParent(null)}>
        <AlertDialogContent data-testid="lessons-delete-category-dialog">
          <AlertDialogHeader>
            <AlertDialogTitle>ลบหมวดหมู่</AlertDialogTitle>
            <AlertDialogDescription>
              ต้องการลบหมวดหมู่ &quot;{pendingDeleteParent?.name}&quot; ใช่หรือไม่?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel data-testid="lessons-delete-category-cancel-button">ยกเลิก</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => void confirmDeleteParent()}
              data-testid="lessons-delete-category-confirm-button"
            >
              ลบหมวดหมู่
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* CD-5 point 5 - replaces window.confirm for archiving a lesson (moving it to trash). */}
      <AlertDialog
        open={pendingArchiveLesson !== null}
        onOpenChange={(next) => !next && setPendingArchiveLesson(null)}
      >
        <AlertDialogContent data-testid="lessons-archive-lesson-dialog">
          <AlertDialogHeader>
            <AlertDialogTitle>ย้ายบทเรียนไปถังขยะ</AlertDialogTitle>
            <AlertDialogDescription>
              ต้องการย้ายบทเรียน &quot;{pendingArchiveLesson?.title}&quot; ไปถังขยะใช่หรือไม่?
              ลิงก์การสอนทั้งหมดของบทเรียนนี้จะถูกยกเลิกทันที
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel data-testid="lessons-archive-lesson-cancel-button">ยกเลิก</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => void confirmArchiveLesson()}
              data-testid="lessons-archive-lesson-confirm-button"
            >
              ย้ายไปถังขยะ
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}

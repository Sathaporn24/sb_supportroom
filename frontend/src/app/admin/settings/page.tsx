"use client";

import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { resolveSectionAccess } from "@/components/admin/settings/section-access";
import { SETTINGS_SECTIONS } from "@/components/admin/settings/sections";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";

/**
 * Pure composition (SP-2/SP-15 ข้อ 4) - this page knows nothing about endpoints, field state, or
 * validation. It only resolves which section is visible for the signed-in role and renders one
 * `Card` per visible section, in registry order. Each section owns its own loading/saving/edit
 * decisions; this page never passes `canEdit` down (SP-15 ข้อ 4).
 */
export default function AdminSettingsPage() {
  const { user, activeCompanyId } = useAdminSession();
  const companyId = activeCompanyId ?? user?.companyId ?? null;
  const role = user?.role ?? null;

  const visibleSections = role
    ? SETTINGS_SECTIONS.filter((section) => resolveSectionAccess(section.access, role).visible)
    : [];

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold">ตั้งค่าบริษัท</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          ค่าที่มีผลกับทั้งบริษัทที่กำลังดูอยู่ แยกบันทึกเป็นรายการ
        </p>
      </div>

      {!companyId ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ยังไม่มีการตั้งค่าที่คุณเข้าถึงได้</EmptyTitle>
            <EmptyDescription>ยังไม่มีบริษัทที่กำลังดูอยู่</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : visibleSections.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ยังไม่มีการตั้งค่าที่คุณเข้าถึงได้</EmptyTitle>
          </EmptyHeader>
        </Empty>
      ) : (
        visibleSections.map(({ id, Component }) => (
          <Component key={`${id}-${companyId}`} companyId={companyId} />
        ))
      )}
    </main>
  );
}

"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { CompanyManagement } from "@/components/admin/CompanyManagement";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { resolveSectionAccess } from "@/components/admin/settings/section-access";
import { SETTINGS_SECTIONS } from "@/components/admin/settings/sections";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

type CompanySettingsTab = "companies" | "settings";

export default function AdminSettingsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { user, activeCompanyId } = useAdminSession();
  const companyId = activeCompanyId ?? user?.companyId ?? null;
  const role = user?.role ?? null;
  const canManageCompanies = role === "owner";

  const visibleSections = role
    ? SETTINGS_SECTIONS.filter((section) => resolveSectionAccess(section.access, role).visible)
    : [];
  const requestedTab = searchParams.get("tab");
  const activeTab: CompanySettingsTab =
    requestedTab === "settings"
      ? "settings"
      : requestedTab === "companies" && canManageCompanies
        ? "companies"
        : canManageCompanies
          ? "companies"
          : "settings";

  function handleTabChange(value: string) {
    const nextTab: CompanySettingsTab = value === "companies" && canManageCompanies ? "companies" : "settings";
    const params = new URLSearchParams(searchParams.toString());
    if (nextTab === "companies") params.delete("tab");
    else params.set("tab", nextTab);
    const query = params.toString();
    router.replace(query ? `/admin/settings?${query}` : "/admin/settings", { scroll: false });
  }

  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-primary">ตั้งค่าบริษัท</h1>
      </div>

      <Tabs value={activeTab} onValueChange={handleTabChange}>
        <TabsList>
          {canManageCompanies && (
            <TabsTrigger value="companies" data-testid="settings-tab-companies">
              จัดการบริษัท
            </TabsTrigger>
          )}
          <TabsTrigger value="settings" data-testid="settings-tab-settings">
            ตั้งค่าบริษัท
          </TabsTrigger>
        </TabsList>

        {canManageCompanies && (
          <TabsContent value="companies" className="pt-4">
            <CompanyManagement />
          </TabsContent>
        )}

        <TabsContent value="settings" className="flex flex-col gap-4 pt-4">
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
        </TabsContent>
      </Tabs>
    </main>
  );
}

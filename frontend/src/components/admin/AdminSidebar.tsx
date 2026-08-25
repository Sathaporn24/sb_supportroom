"use client";

import { usePathname } from "next/navigation";
import {
  FileTextIcon,
  FlagIcon,
  LayoutDashboardIcon,
  MessageCircleQuestionIcon,
  NotebookTextIcon,
  SettingsIcon,
  UsersIcon,
} from "lucide-react";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";
import { Badge } from "@/components/ui/badge";
import { AdminLink } from "@/components/admin/AdminLink";
import { AdminUserMenu } from "@/components/admin/AdminUserMenu";
import { CompanySwitcher } from "@/components/admin/CompanySwitcher";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { useAdminReviewCounts } from "@/hooks/use-admin-review-counts";
import { resolveSectionAccess } from "@/components/admin/settings/section-access";
import { SETTINGS_SECTIONS } from "@/components/admin/settings/sections";

/** Figma's sidebar item spec (node 4001:23374, confirmed via get_design_context): 24px icon,
 * 16px label (bigger than shadcn's default 16px icon/14px text), 8px gap between rows (set on
 * SidebarMenu below). Figma itself only measures the active row at this taller height ("255
 * Fill x 56 Hug" vs 48px for an inactive row, since only the active pill drops the fixed
 * height to hug py-2 + a 24px icon) - user explicitly asked for uniform height across every
 * row instead, using the active row's taller size for all of them, so h-auto/py-2 apply
 * unconditionally here rather than only under data-active. Overridden per item rather than in
 * ui/sidebar.tsx since that file is a pure shadcn primitive and SidebarMenuButton has exactly
 * one consumer (this file).
 *
 * Collapsed (icon-only) state needs its own padding override too: the shared component's
 * collapsed size is a fixed 32px square with 8px padding baked in (`group-data-[collapsible=
 * icon]:size-8! p-2!`), which was tuned for its own default 16px icon (16 + 8+8 = exactly 32,
 * flush). Our icons are 24px instead, so that same 8px padding only leaves a 16px content box
 * for a 24px icon - it doesn't fit, and since flex still anchors it to the left padding edge,
 * the icon overflows entirely out through the right side with zero padding, reading as
 * "shoved against the right wall" instead of centered. 4px (p-1) is what actually centers a
 * 24px icon inside a 32px box (4+24+4=32) - confirmed by measuring getBoundingClientRect() on
 * a live collapsed button before and after, not just eyeballing it. */
const ITEM_CLASS = "h-auto py-2 text-base [&_svg]:size-6 group-data-[collapsible=icon]:p-1!";

function isActivePath(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function AdminSidebar() {
  const pathname = usePathname();
  const { user } = useAdminSession();
  const { queueCount, conflictCount } = useAdminReviewCounts();

  const lessonsActive =
    pathname === "/admin" ||
    isActivePath(pathname, "/admin/lessons") ||
    isActivePath(pathname, "/admin/categories") ||
    isActivePath(pathname, "/admin/links");
  const companySettingsActive =
    isActivePath(pathname, "/admin/settings") || isActivePath(pathname, "/admin/companies");

  // SP-5/SP-15 ข้อ 7 - derive visibility from the same registry the settings page renders.
  const canSeeCompanySettings =
    user?.role != null &&
    SETTINGS_SECTIONS.some((section) => resolveSectionAccess(section.access, user.role).visible);

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <CompanySwitcher />
      </SidebarHeader>
      <SidebarContent>
        {/* Figma's sidebar is one flat list with no section headers - the three
            "เนื้อหาการสอน"/"ตรวจสอบความรู้"/"ตั้งค่า" group labels are dropped. */}
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu className="gap-2">
              <SidebarMenuItem>
                {/* No route exists yet - reserved placement agreed with the project owner, not a
                    real nav item. Kept visibly non-interactive (dashed border, low opacity) so it
                    doesn't read as a broken link. Collapses to the same 32px icon-only square as
                    every real SidebarMenuButton below it (group-data-[collapsible=icon]:size-8
                    matches sidebarMenuButtonVariants' own collapsed size exactly) so the icon rail
                    stays even instead of this one row sticking out taller than its neighbors. */}
                <div className="flex w-full items-center gap-2 rounded-md border border-dashed border-sidebar-border p-2 text-base text-sidebar-foreground/50 group-data-[collapsible=icon]:size-8 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:p-1!">
                  <LayoutDashboardIcon className="size-6 shrink-0" />
                  <span className="flex-1 truncate group-data-[collapsible=icon]:hidden">แดชบอร์ด</span>
                  <Badge
                    variant="secondary"
                    className="pointer-events-none opacity-80 group-data-[collapsible=icon]:hidden"
                  >
                    Phase 2
                  </Badge>
                </div>
              </SidebarMenuItem>

              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={lessonsActive}
                  className={ITEM_CLASS}
                  render={
                    <AdminLink href="/admin/lessons" data-testid="admin-sidebar-lessons-link">
                      <NotebookTextIcon />
                      <span>บทเรียน</span>
                    </AdminLink>
                  }
                />
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={isActivePath(pathname, "/admin/documents")}
                  className={ITEM_CLASS}
                  render={
                    <AdminLink href="/admin/documents" data-testid="admin-sidebar-documents-link">
                      <FileTextIcon />
                      <span>คลังเอกสาร</span>
                    </AdminLink>
                  }
                />
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={isActivePath(pathname, "/admin/qna-queue")}
                  className={ITEM_CLASS}
                  render={
                    <AdminLink href="/admin/qna-queue" data-testid="admin-sidebar-qna-queue-link">
                      <MessageCircleQuestionIcon />
                      <span>คำถามรอคำตอบ</span>
                    </AdminLink>
                  }
                />
                {queueCount > 0 && <SidebarMenuBadge>{queueCount}</SidebarMenuBadge>}
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={isActivePath(pathname, "/admin/qna-conflicts")}
                  className={ITEM_CLASS}
                  render={
                    <AdminLink href="/admin/qna-conflicts" data-testid="admin-sidebar-qna-conflicts-link">
                      <FlagIcon />
                      <span>Q&amp;A ขัดกับเอกสาร</span>
                    </AdminLink>
                  }
                />
                {conflictCount > 0 && <SidebarMenuBadge>{conflictCount}</SidebarMenuBadge>}
              </SidebarMenuItem>
              {user?.role !== "cs" && (
                <SidebarMenuItem>
                  <SidebarMenuButton
                    isActive={isActivePath(pathname, "/admin/users")}
                    className={ITEM_CLASS}
                    render={
                      <AdminLink href="/admin/users" data-testid="admin-sidebar-users-link">
                        <UsersIcon />
                        <span>จัดการผู้ใช้งาน</span>
                      </AdminLink>
                    }
                  />
                </SidebarMenuItem>
              )}
              {canSeeCompanySettings && (
                <SidebarMenuItem>
                  <SidebarMenuButton
                    isActive={companySettingsActive}
                    className={ITEM_CLASS}
                    render={
                      <AdminLink href="/admin/settings" data-testid="admin-sidebar-settings-link">
                        <SettingsIcon />
                        <span>ตั้งค่าบริษัท</span>
                      </AdminLink>
                    }
                  />
                </SidebarMenuItem>
              )}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <AdminUserMenu />
      </SidebarFooter>
    </Sidebar>
  );
}

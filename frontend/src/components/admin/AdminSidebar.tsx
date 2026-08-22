"use client";

import { usePathname } from "next/navigation";
import {
  FileTextIcon,
  FlagIcon,
  FolderTreeIcon,
  Building2Icon,
  LayoutDashboardIcon,
  LinkIcon,
  MessageCircleQuestionIcon,
  NotebookTextIcon,
  SettingsIcon,
  UsersIcon,
} from "lucide-react";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";
import { Badge } from "@/components/ui/badge";
import { AdminLink } from "@/components/admin/AdminLink";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { useAdminReviewCounts } from "@/hooks/use-admin-review-counts";
import { resolveSectionAccess } from "@/components/admin/settings/section-access";
import { SETTINGS_SECTIONS } from "@/components/admin/settings/sections";

function isActivePath(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function AdminSidebar() {
  const pathname = usePathname();
  const { user } = useAdminSession();
  const { queueCount, conflictCount } = useAdminReviewCounts();

  const trainingLinksActive = pathname === "/admin" || isActivePath(pathname, "/admin/links");

  // SP-5/SP-15 ข้อ 7 - แสดงรายการ "ตั้งค่าบริษัท" เมื่อ role นี้เห็นอย่างน้อยหนึ่ง section จริง
  // (derive จาก registry เดียวกับที่หน้า /admin/settings ใช้กรอง) ไม่ hardcode รายชื่อ role ที่นี่
  // เพราะวันที่มี section ที่ซ่อนจาก role หนึ่ง เมนูนี้ต้องตามไปเปลี่ยนเองโดยไม่ต้องแก้ไฟล์นี้อีก
  const canSeeCompanySettings =
    user?.role != null &&
    SETTINGS_SECTIONS.some((section) => resolveSectionAccess(section.access, user.role).visible);

  return (
    <Sidebar collapsible="icon">
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                {/* No route exists yet - reserved placement agreed with the project owner, not a
                    real nav item. Kept visibly non-interactive (dashed border, low opacity) so it
                    doesn't read as a broken link. */}
                <div className="flex w-full items-center gap-2 rounded-md border border-dashed border-sidebar-border p-2 text-sm text-sidebar-foreground/50">
                  <LayoutDashboardIcon className="size-4 shrink-0" />
                  <span className="flex-1 truncate">แดชบอร์ด</span>
                  <Badge variant="secondary" className="pointer-events-none opacity-80">
                    Phase 2
                  </Badge>
                </div>
              </SidebarMenuItem>

              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={trainingLinksActive}
                  render={
                    <AdminLink href="/admin">
                      <LinkIcon />
                      <span>ลิงก์การเรียน</span>
                    </AdminLink>
                  }
                />
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupLabel>เนื้อหาการสอน</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={isActivePath(pathname, "/admin/categories")}
                  render={
                    <AdminLink href="/admin/categories">
                      <FolderTreeIcon />
                      <span>หมวดความรู้</span>
                    </AdminLink>
                  }
                />
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={isActivePath(pathname, "/admin/lessons")}
                  render={
                    <AdminLink href="/admin/lessons">
                      <NotebookTextIcon />
                      <span>บทเรียน</span>
                    </AdminLink>
                  }
                />
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={isActivePath(pathname, "/admin/documents")}
                  render={
                    <AdminLink href="/admin/documents">
                      <FileTextIcon />
                      <span>คลังเอกสาร</span>
                    </AdminLink>
                  }
                />
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupLabel>ตรวจสอบความรู้</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={isActivePath(pathname, "/admin/qna-queue")}
                  render={
                    <AdminLink href="/admin/qna-queue">
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
                  render={
                    <AdminLink href="/admin/qna-conflicts">
                      <FlagIcon />
                      <span>Q&amp;A ขัดกับเอกสาร</span>
                    </AdminLink>
                  }
                />
                {conflictCount > 0 && <SidebarMenuBadge>{conflictCount}</SidebarMenuBadge>}
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        {user?.role === "owner" && (
          <SidebarGroup>
            <SidebarGroupLabel>จัดการระบบ</SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton
                    isActive={isActivePath(pathname, "/admin/companies")}
                    render={
                      <AdminLink href="/admin/companies">
                        <Building2Icon />
                        <span>บริษัททั้งหมด</span>
                      </AdminLink>
                    }
                  />
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        )}

        {(user?.role !== "cs" || canSeeCompanySettings) && (
          <SidebarGroup>
            <SidebarGroupLabel>ตั้งค่า</SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                {user?.role !== "cs" && (
                  <SidebarMenuItem>
                    <SidebarMenuButton
                      isActive={isActivePath(pathname, "/admin/users")}
                      render={
                        <AdminLink href="/admin/users">
                          <UsersIcon />
                          <span>ผู้ใช้งาน</span>
                        </AdminLink>
                      }
                    />
                  </SidebarMenuItem>
                )}
                {canSeeCompanySettings && (
                  <SidebarMenuItem>
                    <SidebarMenuButton
                      isActive={isActivePath(pathname, "/admin/settings")}
                      render={
                        <AdminLink href="/admin/settings">
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
        )}
      </SidebarContent>
    </Sidebar>
  );
}

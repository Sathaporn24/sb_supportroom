"use client";

import { ChevronsUpDownIcon, KeyRoundIcon, LogOutIcon } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { AdminLink } from "@/components/admin/AdminLink";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import type { AdminRole } from "@/types/domain";

const ROLE_LABELS: Record<AdminRole, string> = {
  owner: "เจ้าของ",
  admin: "แอดมิน",
  cs: "ทีม CS",
};

function initialsOf(displayName: string): string {
  return displayName.trim().slice(0, 2).toUpperCase() || "?";
}

export function AdminUserMenu() {
  const { user, signOut } = useAdminSession();
  if (!user) return null;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className="flex w-full items-center gap-2 rounded-lg p-2 text-left outline-none group-data-[collapsible=icon]:justify-center hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 focus-visible:ring-ring/50"
        data-testid="admin-user-menu-trigger"
      >
        <Avatar className="rounded-lg after:rounded-lg">
          <AvatarFallback className="rounded-lg bg-primary text-primary-foreground">
            {initialsOf(user.displayName)}
          </AvatarFallback>
        </Avatar>
        <span className="flex min-w-0 flex-1 flex-col group-data-[collapsible=icon]:hidden">
          <span className="truncate text-sm leading-tight font-medium">{user.displayName}</span>
          <span className="truncate text-xs leading-tight text-muted-foreground">{ROLE_LABELS[user.role]}</span>
        </span>
        <ChevronsUpDownIcon className="size-4 shrink-0 text-sidebar-foreground/50 group-data-[collapsible=icon]:hidden" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" data-testid="admin-user-menu-content">
        <DropdownMenuGroup>
          <DropdownMenuLabel>
            {user.displayName} · {ROLE_LABELS[user.role]}
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          render={
            <AdminLink href="/admin/change-password" data-testid="admin-user-menu-change-password-link">
              <KeyRoundIcon />
              เปลี่ยนรหัสผ่าน
            </AdminLink>
          }
        />
        <DropdownMenuItem variant="destructive" onClick={signOut} data-testid="admin-user-menu-sign-out-button">
          <LogOutIcon />
          ออกจากระบบ
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

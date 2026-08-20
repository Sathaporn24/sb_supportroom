"use client";

import { KeyRoundIcon, LogOutIcon } from "lucide-react";
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
      <DropdownMenuTrigger className="flex items-center gap-2 rounded-md p-1 text-left outline-none hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50">
        <Avatar size="sm">
          <AvatarFallback>{initialsOf(user.displayName)}</AvatarFallback>
        </Avatar>
        <span className="hidden flex-col sm:flex">
          <span className="text-sm leading-tight font-medium">{user.displayName}</span>
          <span className="text-xs leading-tight text-muted-foreground">{ROLE_LABELS[user.role]}</span>
        </span>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuGroup>
          <DropdownMenuLabel>
            {user.displayName} · {ROLE_LABELS[user.role]}
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          render={
            <AdminLink href="/admin/change-password">
              <KeyRoundIcon />
              เปลี่ยนรหัสผ่าน
            </AdminLink>
          }
        />
        <DropdownMenuItem variant="destructive" onClick={signOut}>
          <LogOutIcon />
          ออกจากระบบ
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

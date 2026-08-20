import { SidebarTrigger } from "@/components/ui/sidebar";
import { Separator } from "@/components/ui/separator";
import { CompanySwitcher } from "@/components/admin/CompanySwitcher";
import { AdminUserMenu } from "@/components/admin/AdminUserMenu";

export function AdminTopbar() {
  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b bg-background px-4">
      <SidebarTrigger />
      <Separator orientation="vertical" className="h-5" />
      <span className="text-sm font-semibold whitespace-nowrap">SupportRoom AI</span>
      <div className="mx-2 text-sm text-muted-foreground">
        <CompanySwitcher />
      </div>
      <div className="ml-auto flex items-center gap-3">
        <AdminUserMenu />
      </div>
    </header>
  );
}

import { SidebarTrigger } from "@/components/ui/sidebar";
import { Separator } from "@/components/ui/separator";
import { AdminBreadcrumb } from "@/components/admin/AdminBreadcrumb";

/** Figma's navbar is just the sidebar toggle + a breadcrumb - the company switcher and the user
 * menu that used to live here both moved into the sidebar itself (header and footer). */
export function AdminTopbar() {
  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b bg-background px-4">
      <SidebarTrigger data-testid="admin-topbar-sidebar-toggle-button" />
      <Separator orientation="vertical" className="h-5" />
      <AdminBreadcrumb />
    </header>
  );
}

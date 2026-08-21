import { Suspense } from "react";
import { AdminSessionProvider } from "@/components/admin/AdminSessionProvider";
import { AdminGuard } from "@/components/admin/AdminGuard";
import { TooltipProvider } from "@/components/ui/tooltip";

/**
 * Wraps every /admin route, so a screen added later is signed-in-only and company-aware without
 * anyone remembering to opt in - matching the server, where the default is also "authentication
 * required" and endpoints opt out explicitly.
 *
 * Suspense is required: AdminSessionProvider reads the query string with useSearchParams, which
 * Next.js will not statically render without a boundary.
 */
export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <Suspense fallback={<p className="p-6">กำลังโหลด…</p>}>
      <AdminSessionProvider>
        <TooltipProvider>
          <AdminGuard>{children}</AdminGuard>
        </TooltipProvider>
      </AdminSessionProvider>
    </Suspense>
  );
}

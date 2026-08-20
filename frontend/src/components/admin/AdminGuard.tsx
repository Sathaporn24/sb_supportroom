"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { AdminShell } from "@/components/admin/AdminShell";

/**
 * Keeps signed-out visitors out of the back office and forces a first-sign-in password change
 * before anything else is reachable.
 *
 * ⚠️ This is convenience, not security. Anyone can bypass a client-side redirect; what actually
 * protects the data is that every endpoint requires a verified token server-side (the
 * FallbackPolicy in AuthenticationConfiguration). This exists so a signed-out user sees a login
 * screen instead of a page that renders and then fails every request.
 */
export function AdminGuard({ children }: { children: React.ReactNode }) {
  const { user, ready, activeCompanyId } = useAdminSession();
  const router = useRouter();
  const pathname = usePathname();

  const isLoginPage = pathname === "/admin/login";
  const isChangePasswordPage = pathname === "/admin/change-password";

  useEffect(() => {
    if (!ready) return;

    if (!user && !isLoginPage) {
      router.replace("/admin/login");
      return;
    }
    // A seeded or newly-created account's password is known to whoever set it, so nothing else
    // opens until it has been replaced.
    if (user?.mustChangePassword && !isChangePasswordPage) {
      router.replace("/admin/change-password");
    }
  }, [ready, user, isLoginPage, isChangePasswordPage, router]);

  if (!ready) return <p style={{ padding: 24 }}>กำลังตรวจสอบสิทธิ์…</p>;
  if (isLoginPage) return <>{children}</>;
  if (!user) return null;
  if (user.mustChangePassword && !isChangePasswordPage) return null;

  // Every implemented work screen is company-scoped. An owner has no company baked into the
  // token, so rendering a child before they choose one only fires requests that must fail with
  // "company unknown". Keep the navigation/switcher visible and hold the work screen until the
  // URL has a real context. Future system-wide routes (provider settings/company registry) should
  // be explicitly exempted here when they are implemented.
  if (user.role === "owner" && !activeCompanyId && !isChangePasswordPage) {
    return (
      <AdminShell>
        <main className="mx-auto flex max-w-4xl flex-col gap-2 p-6">
          <h1 className="text-lg font-semibold">เลือกบริษัทก่อนเริ่มทำงาน</h1>
          <p className="text-sm text-muted-foreground">
            กรุณาเลือกบริษัทจากแถบด้านบน เพื่อให้ทุกหน้ารู้ว่ากำลังดูข้อมูลของใคร
          </p>
        </main>
      </AdminShell>
    );
  }

  return <AdminShell>{children}</AdminShell>;
}

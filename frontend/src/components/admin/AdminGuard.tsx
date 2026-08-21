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
  const { user, ready } = useAdminSession();
  const router = useRouter();
  const pathname = usePathname();

  const isLoginPage = pathname === "/admin/login";
  const isChangePasswordPage = pathname === "/admin/change-password";
  const isOwnerOnlyPage = pathname === "/admin/companies" || pathname.startsWith("/admin/companies/");

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
    // No redirect here, unlike the checks above: a non-owner on an owner-only page still gets a
    // rendered page (the message below), matching how /admin/users handles a role without
    // permission - an inline explanation, not a silent bounce back to /admin.
  }, [ready, user, isLoginPage, isChangePasswordPage, router]);

  if (!ready) return <p className="p-6">กำลังตรวจสอบสิทธิ์…</p>;
  if (isLoginPage) return <>{children}</>;
  if (!user) return null;
  if (user.mustChangePassword && !isChangePasswordPage) return null;

  if (isOwnerOnlyPage && user.role !== "owner") {
    return (
      <AdminShell>
        <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
          <p className="text-sm text-muted-foreground">ไม่มีสิทธิ์เข้าถึงหน้านี้</p>
        </main>
      </AdminShell>
    );
  }

  return <AdminShell>{children}</AdminShell>;
}

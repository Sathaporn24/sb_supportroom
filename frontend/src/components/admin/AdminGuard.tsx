"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { CompanySwitcher } from "@/components/admin/CompanySwitcher";

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

  return (
    <>
      <AdminBar />
      {children}
    </>
  );
}

/**
 * Deliberately unstyled - plain HTML with the minimum inline layout needed to be usable. The
 * whole back office is being redesigned in Figma and rebuilt with shadcn/ui, so any styling
 * invested here is thrown away twice: once when the design lands, once when the components change.
 */
function AdminBar() {
  const { user, signOut } = useAdminSession();
  if (!user) return null;

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 16,
        flexWrap: "wrap",
        padding: "8px 16px",
        borderBottom: "1px solid #ccc",
        fontSize: 14,
      }}
    >
      <CompanySwitcher />
      <span style={{ marginLeft: "auto" }}>
        {user.displayName} ({user.role})
      </span>
      {user.role !== "cs" && <a href="/admin/users">จัดการผู้ใช้</a>}
      <a href="/admin/change-password">เปลี่ยนรหัสผ่าน</a>
      <button type="button" onClick={signOut}>
        ออกจากระบบ
      </button>
    </div>
  );
}

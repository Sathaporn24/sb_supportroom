"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";

/** Unstyled on purpose - see the note on the login page. */
export default function ChangePasswordPage() {
  const { user, refreshUser } = useAdminSession();
  const router = useRouter();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const forced = user?.mustChangePassword ?? false;

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);

    // Checked here rather than server-side: the two fields only exist to catch a typo, and the
    // API has no use for a confirmation it cannot verify anything about.
    if (newPassword !== confirmPassword) {
      setError("รหัสผ่านใหม่ทั้งสองช่องไม่ตรงกัน");
      return;
    }

    setSubmitting(true);
    try {
      await api.changePassword({ currentPassword, newPassword });
      // Re-read the profile so mustChangePassword clears and AdminGuard stops holding them here.
      await refreshUser();
      router.replace("/admin");
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.message : "เปลี่ยนรหัสผ่านไม่สำเร็จ");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main style={{ maxWidth: 360, margin: "10vh auto", padding: 24 }}>
      <h1>เปลี่ยนรหัสผ่าน</h1>
      {forced && (
        <p>
          บัญชีนี้ยังใช้รหัสผ่านเริ่มต้นที่ผู้สร้างบัญชีเป็นคนตั้ง กรุณาเปลี่ยนก่อนใช้งาน
        </p>
      )}
      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 12, marginTop: 16 }}>
        <label style={{ display: "grid", gap: 4 }}>
          รหัสผ่านปัจจุบัน
          <input
            type="password"
            value={currentPassword}
            onChange={(event) => setCurrentPassword(event.target.value)}
            required
            autoComplete="current-password"
          />
        </label>
        <label style={{ display: "grid", gap: 4 }}>
          รหัสผ่านใหม่ (อย่างน้อย 10 ตัวอักษร)
          <input
            type="password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            required
            minLength={10}
            autoComplete="new-password"
          />
        </label>
        <label style={{ display: "grid", gap: 4 }}>
          ยืนยันรหัสผ่านใหม่
          <input
            type="password"
            value={confirmPassword}
            onChange={(event) => setConfirmPassword(event.target.value)}
            required
            autoComplete="new-password"
          />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? "กำลังบันทึก…" : "บันทึก"}
        </button>
      </form>
    </main>
  );
}

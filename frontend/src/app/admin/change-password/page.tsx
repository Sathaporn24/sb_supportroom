"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";

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
    <main className="flex w-full flex-col gap-6 p-6">
      <Card>
        <CardHeader>
          <CardTitle className="text-xl">เปลี่ยนรหัสผ่าน</CardTitle>
          {forced && (
            <CardDescription>
              บัญชีนี้ยังใช้รหัสผ่านเริ่มต้นที่ผู้สร้างบัญชีเป็นคนตั้ง กรุณาเปลี่ยนก่อนใช้งาน
            </CardDescription>
          )}
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="current-password">รหัสผ่านปัจจุบัน</Label>
              <Input
                id="current-password"
                type="password"
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
                required
                autoComplete="current-password"
                autoFocus
                data-testid="change-password-current-input"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="new-password">รหัสผ่านใหม่ (อย่างน้อย 10 ตัวอักษร)</Label>
              <Input
                id="new-password"
                type="password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                required
                minLength={10}
                autoComplete="new-password"
                data-testid="change-password-new-input"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="confirm-password">ยืนยันรหัสผ่านใหม่</Label>
              <Input
                id="confirm-password"
                type="password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                required
                autoComplete="new-password"
                data-testid="change-password-confirm-input"
              />
            </div>
            {error && (
              <Alert variant="destructive" role="alert">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
            <Button type="submit" disabled={submitting} className="mt-2" data-testid="change-password-submit-button">
              {submitting ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังบันทึก...
                </>
              ) : (
                "บันทึก"
              )}
            </Button>
          </form>
        </CardContent>
      </Card>
    </main>
  );
}

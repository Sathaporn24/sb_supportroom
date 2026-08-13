"use client";

import { useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";

/**
 * Unstyled on purpose. This screen is going through Figma and will be rebuilt with shadcn/ui;
 * styling it now means designing it twice. It is here so the permission rules can be exercised
 * end to end today.
 */
export default function LoginPage() {
  const { signIn } = useAdminSession();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const { result } = await api.login({ email: email.trim(), password });
      signIn(result);
    } catch (caught) {
      // The API answers wrong-email and wrong-password identically so this screen cannot be used
      // to discover which addresses have accounts - show what it said, do not elaborate.
      setError(caught instanceof ApiClientError ? caught.message : "เข้าสู่ระบบไม่สำเร็จ");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main style={{ maxWidth: 360, margin: "10vh auto", padding: 24 }}>
      <h1>เข้าสู่ระบบ</h1>
      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 12, marginTop: 16 }}>
        <label style={{ display: "grid", gap: 4 }}>
          อีเมล
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
            autoComplete="username"
          />
        </label>
        <label style={{ display: "grid", gap: 4 }}>
          รหัสผ่าน
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            autoComplete="current-password"
          />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? "กำลังเข้าสู่ระบบ…" : "เข้าสู่ระบบ"}
        </button>
      </form>
    </main>
  );
}

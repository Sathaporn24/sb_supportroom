"use client";

import { useCallback, useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import type { AdminRole, AdminUser } from "@/types/domain";

/**
 * Add and deactivate the people who can sign in to one company's back office.
 *
 * Unstyled on purpose - see the note on the login page.
 *
 * The role options offered here stop at the signed-in user's own level, matching the server rule
 * that nobody may hand out a role above their own. Hiding the option is a courtesy so the choice
 * never appears and then fails; the check that matters is server-side, in IAuthorizationGuard.
 */
export default function AdminUsersPage() {
  const { user, activeCompanyId } = useAdminSession();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const companyId = activeCompanyId ?? user?.companyId ?? null;

  const assignableRoles: AdminRole[] = user?.role === "owner" ? ["owner", "admin", "cs"] : ["admin", "cs"];

  const reload = useCallback(async () => {
    if (!companyId) {
      // An owner who has not picked a customer yet. There is no sensible list to show, and
      // guessing one would silently pick a customer for them.
      setUsers([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const { users: list } = await api.listAdminUsers(companyId);
      setUsers(list);
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.message : "โหลดรายชื่อผู้ใช้ไม่สำเร็จ");
    } finally {
      setLoading(false);
    }
  }, [companyId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (user?.role === "cs") {
    return (
      <main style={{ padding: 24 }}>
        <p>บัญชีของคุณไม่มีสิทธิ์จัดการผู้ใช้</p>
      </main>
    );
  }

  return (
    <main style={{ padding: 24, display: "grid", gap: 24 }}>
      <h1>จัดการผู้ใช้</h1>

      {!companyId && <p>กรุณาเลือกบริษัทจากแถบด้านบนก่อน</p>}
      {error && <p role="alert">{error}</p>}

      {companyId && (
        <>
          <CreateUserForm companyId={companyId} assignableRoles={assignableRoles} onCreated={reload} />

          {loading ? (
            <p>กำลังโหลด…</p>
          ) : (
            <table style={{ borderCollapse: "collapse" }}>
              <thead>
                <tr>
                  <th style={cell}>ชื่อ</th>
                  <th style={cell}>อีเมล</th>
                  <th style={cell}>สิทธิ์</th>
                  <th style={cell}>สถานะ</th>
                  <th style={cell}>เข้าใช้ล่าสุด</th>
                  <th style={cell} />
                </tr>
              </thead>
              <tbody>
                {users.length === 0 && (
                  <tr>
                    <td style={cell} colSpan={6}>
                      ยังไม่มีผู้ใช้ในบริษัทนี้
                    </td>
                  </tr>
                )}
                {users.map((row) => (
                  <UserRow key={row.id} row={row} assignableRoles={assignableRoles} onChanged={reload} />
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </main>
  );
}

const cell: React.CSSProperties = { border: "1px solid #ccc", padding: "4px 8px", textAlign: "left" };

function UserRow({
  row,
  assignableRoles,
  onChanged,
}: {
  row: AdminUser;
  assignableRoles: AdminRole[];
  onChanged: () => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function apply(changes: Partial<{ role: AdminRole; isActive: boolean }>) {
    setBusy(true);
    setError(null);
    try {
      await api.updateAdminUser(row.id, {
        displayName: row.displayName,
        role: changes.role ?? row.role,
        isActive: changes.isActive ?? row.isActive,
      });
      await onChanged();
    } catch (caught) {
      // Expected here, not exceptional: this is where "cannot deactivate the last admin" and
      // "cannot assign a role above your own" surface. Show the server's reason as-is.
      setError(caught instanceof ApiClientError ? caught.message : "บันทึกไม่สำเร็จ");
    } finally {
      setBusy(false);
    }
  }

  return (
    <tr>
      <td style={cell}>{row.displayName}</td>
      <td style={cell}>{row.email}</td>
      <td style={cell}>
        <select
          value={row.role}
          disabled={busy}
          onChange={(event) => void apply({ role: event.target.value as AdminRole })}
        >
          {/* Include the current role even when it is above what this user may assign, so the
              row shows the truth rather than silently mis-reporting someone as lower-ranked. */}
          {[...new Set([row.role, ...assignableRoles])].map((role) => (
            <option key={role} value={role} disabled={!assignableRoles.includes(role)}>
              {role}
            </option>
          ))}
        </select>
      </td>
      <td style={cell}>{row.isActive ? "ใช้งานอยู่" : "ปิดใช้งาน"}</td>
      <td style={cell}>{row.lastLoginAt ? new Date(row.lastLoginAt).toLocaleString("th-TH") : "—"}</td>
      <td style={cell}>
        <button type="button" disabled={busy} onClick={() => void apply({ isActive: !row.isActive })}>
          {row.isActive ? "ปิดบัญชี" : "เปิดบัญชี"}
        </button>
        {error && <span role="alert"> {error}</span>}
      </td>
    </tr>
  );
}

function CreateUserForm({
  companyId,
  assignableRoles,
  onCreated,
}: {
  companyId: string;
  assignableRoles: AdminRole[];
  onCreated: () => Promise<void>;
}) {
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [role, setRole] = useState<AdminRole>("cs");
  const [initialPassword, setInitialPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await api.createAdminUser({
        email: email.trim(),
        displayName: displayName.trim(),
        role,
        companyId,
        initialPassword,
      });
      setEmail("");
      setDisplayName("");
      setInitialPassword("");
      await onCreated();
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.message : "เพิ่มผู้ใช้ไม่สำเร็จ");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: "grid", gap: 8, maxWidth: 420 }}>
      <h2>เพิ่มผู้ใช้</h2>
      <input
        type="email"
        placeholder="อีเมล"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        required
      />
      <input
        placeholder="ชื่อที่แสดง"
        value={displayName}
        onChange={(event) => setDisplayName(event.target.value)}
        required
      />
      <select value={role} onChange={(event) => setRole(event.target.value as AdminRole)}>
        {assignableRoles.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <input
        type="password"
        placeholder="รหัสผ่านเริ่มต้น (อย่างน้อย 10 ตัวอักษร)"
        value={initialPassword}
        onChange={(event) => setInitialPassword(event.target.value)}
        required
        minLength={10}
        autoComplete="new-password"
      />
      <p>ผู้ใช้ใหม่จะต้องเปลี่ยนรหัสผ่านนี้ทันทีที่เข้าสู่ระบบครั้งแรก</p>
      {error && <p role="alert">{error}</p>}
      <button type="submit" disabled={submitting}>
        {submitting ? "กำลังเพิ่ม…" : "เพิ่มผู้ใช้"}
      </button>
    </form>
  );
}

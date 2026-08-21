"use client";

import { useCallback, useEffect, useState } from "react";
import { PlusIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import type { AdminRole, AdminUser } from "@/types/domain";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TableSkeleton } from "@/components/shared/TableSkeleton";

const ROLE_LABELS: Record<AdminRole, string> = {
  owner: "เจ้าของ",
  admin: "แอดมิน",
  cs: "ทีม CS",
};

/**
 * Add and deactivate the people who can sign in to one company's back office.
 *
 * The role options offered here stop at the signed-in user's own level, matching the server rule
 * that nobody may hand out a role above their own. Hiding the option is a courtesy so the choice
 * never appears and then fails; the check that matters is server-side, in IAuthorizationGuard.
 */
export default function AdminUsersPage() {
  const { user, activeCompanyId, companies } = useAdminSession();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [createOpen, setCreateOpen] = useState(false);

  const companyId = activeCompanyId ?? user?.companyId ?? null;

  const assignableRoles: AdminRole[] = user?.role === "owner" ? ["owner", "admin", "cs"] : ["admin", "cs"];

  const reload = useCallback(async () => {
    if (!companyId) {
      // A company-scoped user always has companyId set from their own session, so this only fires
      // for an owner: either briefly, before AdminSessionProvider's auto-select effect resolves
      // the first company once the companies fetch comes back, or persistently if the company
      // registry has genuinely zero companies. There is no manual "pick a company" step to send
      // the owner to anymore - the effect does it automatically - so there is no sensible list to
      // show either way.
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
      <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
        <p className="text-sm text-muted-foreground">บัญชีของคุณไม่มีสิทธิ์จัดการผู้ใช้</p>
      </main>
    );
  }

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold">จัดการผู้ใช้</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            เพิ่ม กำหนดสิทธิ์ และปิดการใช้งานบัญชีที่เข้าระบบหลังบ้านของบริษัทนี้
          </p>
        </div>
        {companyId && (
          <Button onClick={() => setCreateOpen(true)}>
            <PlusIcon data-icon="inline-start" />
            เพิ่มผู้ใช้
          </Button>
        )}
      </div>

      {!companyId && (
        <p className="text-sm text-muted-foreground">
          {companies.length === 0 ? "ยังไม่มีบริษัทในระบบ" : "กำลังโหลดข้อมูลบริษัท..."}
        </p>
      )}
      {error && (
        <Alert variant="destructive" role="alert">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {companyId && (
        <>
          {loading ? (
            <TableSkeleton columns={6} />
          ) : users.length === 0 ? (
            <Empty className="border">
              <EmptyHeader>
                <EmptyTitle>ยังไม่มีผู้ใช้ในบริษัทนี้</EmptyTitle>
                <EmptyDescription>เริ่มด้วยการเพิ่มผู้ใช้คนแรก</EmptyDescription>
              </EmptyHeader>
            </Empty>
          ) : (
            <div className="overflow-hidden rounded-xl border">
              <Table className="min-w-[640px]">
                <TableHeader>
                  <TableRow>
                    <TableHead className="px-4">ชื่อ</TableHead>
                    <TableHead className="px-4">อีเมล</TableHead>
                    <TableHead className="px-4">สิทธิ์</TableHead>
                    <TableHead className="px-4">สถานะ</TableHead>
                    <TableHead className="px-4">เข้าใช้ล่าสุด</TableHead>
                    <TableHead className="px-4" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((row) => (
                    <UserRow key={row.id} row={row} assignableRoles={assignableRoles} onChanged={reload} />
                  ))}
                </TableBody>
              </Table>
            </div>
          )}

          <CreateUserDialog
            open={createOpen}
            onClose={() => setCreateOpen(false)}
            companyId={companyId}
            assignableRoles={assignableRoles}
            onCreated={reload}
          />
        </>
      )}
    </main>
  );
}

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

  // Include the current role even when it is above what this user may assign, so the row shows
  // the truth rather than silently mis-reporting someone as lower-ranked.
  const roleOptions = [...new Set([row.role, ...assignableRoles])];

  return (
    <TableRow>
      <TableCell className="px-4 py-3 font-medium">{row.displayName}</TableCell>
      <TableCell className="px-4 py-3 text-muted-foreground">{row.email}</TableCell>
      <TableCell className="px-4 py-3">
        <Select
          value={row.role}
          disabled={busy}
          onValueChange={(value) => value && void apply({ role: value as AdminRole })}
        >
          <SelectTrigger size="sm">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {roleOptions.map((role) => (
              <SelectItem key={role} value={role} disabled={!assignableRoles.includes(role)}>
                {ROLE_LABELS[role]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </TableCell>
      <TableCell className="px-4 py-3">
        <Badge variant={row.isActive ? "default" : "secondary"}>{row.isActive ? "ใช้งานอยู่" : "ปิดใช้งาน"}</Badge>
      </TableCell>
      <TableCell className="px-4 py-3 text-muted-foreground">
        {row.lastLoginAt ? new Date(row.lastLoginAt).toLocaleString("th-TH") : "—"}
      </TableCell>
      <TableCell className="px-4 py-3">
        <div className="flex flex-col items-end gap-1">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={busy}
            onClick={() => void apply({ isActive: !row.isActive })}
          >
            {row.isActive ? "ปิดบัญชี" : "เปิดบัญชี"}
          </Button>
          {error && <p className="text-xs text-destructive">{error}</p>}
        </div>
      </TableCell>
    </TableRow>
  );
}

function CreateUserDialog({
  open,
  onClose,
  companyId,
  assignableRoles,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
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

  useEffect(() => {
    if (!open) return;
    setEmail("");
    setDisplayName("");
    setRole(assignableRoles.includes("cs") ? "cs" : assignableRoles[0]);
    setInitialPassword("");
    setError(null);
    setSubmitting(false);
    // assignableRoles is derived from the signed-in user's own role, which does not change while
    // the dialog is open - only re-run this when the dialog itself opens.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

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
      await onCreated();
      onClose();
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.message : "เพิ่มผู้ใช้ไม่สำเร็จ");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>เพิ่มผู้ใช้</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="new-user-email">อีเมล</Label>
            <Input
              id="new-user-email"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="new-user-name">ชื่อที่แสดง</Label>
            <Input
              id="new-user-name"
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              required
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="new-user-role">สิทธิ์</Label>
            <Select value={role} onValueChange={(value) => value && setRole(value as AdminRole)}>
              <SelectTrigger id="new-user-role" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {assignableRoles.map((option) => (
                  <SelectItem key={option} value={option}>
                    {ROLE_LABELS[option]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="new-user-password">รหัสผ่านเริ่มต้น (อย่างน้อย 10 ตัวอักษร)</Label>
            <Input
              id="new-user-password"
              type="password"
              value={initialPassword}
              onChange={(event) => setInitialPassword(event.target.value)}
              required
              minLength={10}
              autoComplete="new-password"
            />
            <p className="text-xs text-muted-foreground">ผู้ใช้ใหม่จะต้องเปลี่ยนรหัสผ่านนี้ทันทีที่เข้าสู่ระบบครั้งแรก</p>
          </div>
          {error && (
            <Alert variant="destructive" role="alert">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
          <Button type="submit" disabled={submitting}>
            {submitting ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังเพิ่ม...
              </>
            ) : (
              "เพิ่มผู้ใช้"
            )}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

"use client";

import { useState } from "react";
import { CheckCircle2Icon } from "lucide-react";
import { AdminLink } from "@/components/admin/AdminLink";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Spinner } from "@/components/ui/spinner";
import { ApiClientError, createCompany } from "@/lib/api-client";
import { getCompanyCreateFieldErrors, type CompanyCreateFieldErrors } from "@/lib/company-provisioning";

type CreatedCompanySummary = {
  id: string;
  name: string;
  adminEmail: string;
  adminInitialPassword: string;
};

export default function NewCompanyPage() {
  const [id, setId] = useState("");
  const [name, setName] = useState("");
  const [adminEmail, setAdminEmail] = useState("");
  const [adminDisplayName, setAdminDisplayName] = useState("");
  const [adminInitialPassword, setAdminInitialPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<CompanyCreateFieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<CreatedCompanySummary | null>(null);

  function resetForNextCompany() {
    setId("");
    setName("");
    setAdminEmail("");
    setAdminDisplayName("");
    setAdminInitialPassword("");
    setFieldErrors({});
    setFormError(null);
    setCreated(null);
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFieldErrors({});
    setFormError(null);
    setSubmitting(true);

    const normalizedId = id.trim().toLowerCase();
    const normalizedName = name.trim();
    const normalizedEmail = adminEmail.trim();
    const normalizedDisplayName = adminDisplayName.trim();

    try {
      // CP-10: deliberately ignore the response. The initial password below comes only from this
      // form's in-memory state and is never expected back from the API.
      await createCompany({
        id: normalizedId,
        name: normalizedName,
        adminEmail: normalizedEmail,
        adminDisplayName: normalizedDisplayName,
        adminInitialPassword,
      });
      setCreated({
        id: normalizedId,
        name: normalizedName,
        adminEmail: normalizedEmail,
        adminInitialPassword,
      });
    } catch (caught) {
      const message = caught instanceof ApiClientError ? caught.message : "สร้างบริษัทไม่สำเร็จ";
      const errors = getCompanyCreateFieldErrors(message);
      if (errors) setFieldErrors(errors);
      else setFormError(message);
    } finally {
      setSubmitting(false);
    }
  }

  if (created) {
    return (
      <main className="mx-auto flex max-w-2xl flex-col gap-6 p-6">
        <Alert>
          <CheckCircle2Icon />
          <AlertTitle>สร้างบริษัทเรียบร้อยแล้ว</AlertTitle>
          <AlertDescription>
            บริษัทและบัญชี admin คนแรกพร้อมใช้งาน ผู้ใช้จะถูกบังคับให้เปลี่ยนรหัสผ่านเมื่อเข้าสู่ระบบครั้งแรก
          </AlertDescription>
        </Alert>

        <Card>
          <CardHeader>
            <CardTitle>ข้อมูลสำหรับแจ้งลูกค้า</CardTitle>
            <CardDescription>
              ข้อมูลนี้มาจากฟอร์มในหน้านี้เท่านั้น ระบบไม่ได้ส่งรหัสผ่านกลับมาจาก API
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <SummaryField label="บริษัท" value={`${created.name} (${created.id})`} />
            <SummaryField label="อีเมล admin" value={created.adminEmail} />
            <SummaryField label="รหัสผ่านเริ่มต้น" value={created.adminInitialPassword} />
          </CardContent>
          <CardFooter className="flex flex-wrap gap-2">
            <AdminLink href="/admin/companies" className={buttonVariants()}>
              กลับไปรายการบริษัท
            </AdminLink>
            <Button type="button" variant="outline" onClick={resetForNextCompany}>
              สร้างบริษัทถัดไป
            </Button>
          </CardFooter>
        </Card>
      </main>
    );
  }

  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold">สร้างบริษัทใหม่</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          สร้างบริษัท บัญชี admin คนแรก และหมวดความรู้เริ่มต้นพร้อมกันในครั้งเดียว
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>ข้อมูลบริษัทและผู้ดูแลคนแรก</CardTitle>
          <CardDescription>ทุกช่องจำเป็น รหัสบริษัทจะถูกใช้ใน URL และแก้ภายหลังไม่ได้</CardDescription>
        </CardHeader>
        <CardContent>
          <form id="create-company-form" onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="company-id">รหัสบริษัท</Label>
              <Input
                id="company-id"
                value={id}
                onChange={(event) => {
                  setId(event.target.value.toLowerCase());
                  setFieldErrors((current) => ({ ...current, id: undefined }));
                }}
                required
                maxLength={40}
                pattern="[a-z0-9]+(-[a-z0-9]+)*"
                placeholder="เช่น scb หรือ school-bright"
                aria-invalid={Boolean(fieldErrors.id)}
                aria-describedby={fieldErrors.id ? "company-id-error" : "company-id-help"}
              />
              {fieldErrors.id ? (
                <p id="company-id-error" className="text-xs text-destructive">
                  {fieldErrors.id}
                </p>
              ) : (
                <p id="company-id-help" className="text-xs text-muted-foreground">
                  ใช้เฉพาะ a-z, 0-9 และขีดกลาง สูงสุด 40 ตัวอักษร ห้ามใส่ข้อมูลลับเพราะค่านี้ปรากฏใน URL
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="company-name">ชื่อบริษัท</Label>
              <Input
                id="company-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                required
                maxLength={200}
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="admin-email">อีเมล admin คนแรก</Label>
              <Input
                id="admin-email"
                type="email"
                value={adminEmail}
                onChange={(event) => {
                  setAdminEmail(event.target.value);
                  setFieldErrors((current) => ({ ...current, adminEmail: undefined }));
                }}
                required
                aria-invalid={Boolean(fieldErrors.adminEmail)}
                aria-describedby={fieldErrors.adminEmail ? "admin-email-error" : undefined}
              />
              {fieldErrors.adminEmail && (
                <p id="admin-email-error" className="text-xs text-destructive">
                  {fieldErrors.adminEmail}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="admin-display-name">ชื่อที่แสดงของ admin</Label>
              <Input
                id="admin-display-name"
                value={adminDisplayName}
                onChange={(event) => setAdminDisplayName(event.target.value)}
                required
                maxLength={100}
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="admin-initial-password">รหัสผ่านเริ่มต้น</Label>
              <Input
                id="admin-initial-password"
                type="password"
                value={adminInitialPassword}
                onChange={(event) => setAdminInitialPassword(event.target.value)}
                required
                minLength={10}
                autoComplete="new-password"
              />
              <p className="text-xs text-muted-foreground">
                อย่างน้อย 10 ตัวอักษร และ admin จะต้องเปลี่ยนรหัสนี้ทันทีที่เข้าสู่ระบบครั้งแรก
              </p>
            </div>

            {formError && (
              <Alert variant="destructive">
                <AlertDescription>{formError}</AlertDescription>
              </Alert>
            )}
          </form>
        </CardContent>
        <CardFooter className="flex flex-wrap gap-2">
          <Button type="submit" form="create-company-form" disabled={submitting}>
            {submitting ? (
              <>
                <Spinner data-icon="inline-start" />
                กำลังสร้าง...
              </>
            ) : (
              "สร้างบริษัท"
            )}
          </Button>
          <AdminLink href="/admin/companies" className={buttonVariants({ variant: "outline" })}>
            ยกเลิก
          </AdminLink>
        </CardFooter>
      </Card>
    </main>
  );
}

function SummaryField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-2">
      <Label>{label}</Label>
      <Input value={value} readOnly />
    </div>
  );
}

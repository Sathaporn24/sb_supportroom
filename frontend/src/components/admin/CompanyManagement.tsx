"use client";

import { useCallback, useEffect, useState } from "react";
import { PlusIcon } from "lucide-react";
import { AdminLink } from "@/components/admin/AdminLink";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/utils";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TableSkeleton } from "@/components/shared/TableSkeleton";
import { ApiClientError, listAllCompanies, updateCompany } from "@/lib/api-client";
import { DEACTIVATE_COMPANY_CONFIRMATION } from "@/lib/company-provisioning";
import type { Company } from "@/types/domain";

export function CompanyManagement() {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [busyCompanyId, setBusyCompanyId] = useState<string | null>(null);
  const [companyToDeactivate, setCompanyToDeactivate] = useState<Company | null>(null);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      // CP-13: this owner registry intentionally uses /all, never the switcher's active-only route.
      const { companies: list } = await listAllCompanies();
      setCompanies(list);
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.message : "โหลดรายการบริษัทไม่สำเร็จ");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function setCompanyActive(company: Company, isActive: boolean) {
    setBusyCompanyId(company.id);
    setError(null);
    try {
      await updateCompany(company.id, { name: company.name, isActive });
      setCompanies((current) =>
        current.map((item) => (item.id === company.id ? { ...item, isActive } : item)),
      );
      setCompanyToDeactivate(null);
    } catch (caught) {
      setError(caught instanceof ApiClientError ? caught.message : "บันทึกสถานะบริษัทไม่สำเร็จ");
    } finally {
      setBusyCompanyId(null);
    }
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <p className="text-sm text-muted-foreground">
          ดูบริษัททั้งที่ใช้งานอยู่และปิดใช้งานแล้ว พร้อมเปิดหรือปิดการเข้าสู่ระบบของพนักงาน
        </p>
        <AdminLink
          href="/admin/companies/new"
          data-testid="companies-create-new-link"
          className={cn(buttonVariants())}
        >
          <PlusIcon data-icon="inline-start" />
          สร้างบริษัทใหม่
        </AdminLink>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {loading ? (
        <TableSkeleton columns={4} />
      ) : companies.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>ยังไม่มีบริษัท</EmptyTitle>
            <EmptyDescription>เริ่มรับลูกค้ารายแรกด้วยปุ่มสร้างบริษัทใหม่</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="overflow-hidden rounded-xl border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="px-4">รหัสบริษัท</TableHead>
                <TableHead className="px-4">ชื่อบริษัท</TableHead>
                <TableHead className="px-4">สถานะ</TableHead>
                <TableHead className="px-4 text-right">การจัดการ</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {companies.map((company) => {
                const busy = busyCompanyId === company.id;
                return (
                  <TableRow key={company.id} data-testid={`company-row-${company.id}`}>
                    <TableCell className="px-4 py-3 font-mono text-xs">{company.id}</TableCell>
                    <TableCell className="px-4 py-3 font-medium">{company.name}</TableCell>
                    <TableCell className="px-4 py-3">
                      <Badge variant={company.isActive ? "default" : "secondary"}>
                        {company.isActive ? "ใช้งานอยู่" : "ปิดใช้งาน"}
                      </Badge>
                    </TableCell>
                    <TableCell className="px-4 py-3 text-right">
                      <Button
                        type="button"
                        size="sm"
                        variant={company.isActive ? "destructive" : "outline"}
                        disabled={busyCompanyId !== null}
                        data-testid={`company-row-${company.id}-toggle-active-button`}
                        onClick={() =>
                          company.isActive
                            ? setCompanyToDeactivate(company)
                            : void setCompanyActive(company, true)
                        }
                      >
                        {busy ? (
                          <>
                            <Spinner data-icon="inline-start" />
                            กำลังบันทึก...
                          </>
                        ) : company.isActive ? (
                          "ปิดใช้งาน"
                        ) : (
                          "เปิดใช้งาน"
                        )}
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}

      <AlertDialog
        open={companyToDeactivate !== null}
        onOpenChange={(open) => !open && busyCompanyId === null && setCompanyToDeactivate(null)}
      >
        <AlertDialogContent data-testid="deactivate-company-dialog">
          <AlertDialogHeader>
            <AlertDialogTitle>ปิดใช้งาน {companyToDeactivate?.name}?</AlertDialogTitle>
            <AlertDialogDescription>{DEACTIVATE_COMPANY_CONFIRMATION}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel data-testid="deactivate-company-dialog-cancel-button" disabled={busyCompanyId !== null}>
              ยกเลิก
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={busyCompanyId !== null || companyToDeactivate === null}
              data-testid="deactivate-company-dialog-confirm-button"
              onClick={() => companyToDeactivate && void setCompanyActive(companyToDeactivate, false)}
            >
              {busyCompanyId ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังปิด...
                </>
              ) : (
                "ยืนยันปิดใช้งาน"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  );
}

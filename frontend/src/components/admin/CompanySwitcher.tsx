"use client";

import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

/**
 * Picks which customer's data the back office is showing.
 *
 * Hidden when there is nothing to choose between - a company-scoped user belongs to exactly one
 * customer, and a dropdown with a single option is just a control that does nothing.
 */
export function CompanySwitcher() {
  const { companies, activeCompanyId, switchCompany, user } = useAdminSession();

  if (companies.length <= 1) {
    // Still name the customer being viewed. Without it there is no on-screen answer to "whose
    // data am I looking at right now?", which is the question the whole switcher exists for.
    const only = companies[0] ?? null;
    return only ? <span>บริษัท: {only.name}</span> : null;
  }

  return (
    <Select
      value={activeCompanyId ?? user?.companyId ?? ""}
      onValueChange={(value) => value && switchCompany(value)}
    >
      <SelectTrigger size="sm" aria-label="เลือกบริษัท">
        <SelectValue placeholder="— เลือกบริษัท —" />
      </SelectTrigger>
      <SelectContent>
        {companies.map((company) => (
          <SelectItem key={company.id} value={company.id}>
            {company.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

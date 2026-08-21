"use client";

import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

/**
 * Picks which customer's data the back office is showing.
 *
 * Only `owner` ever switches between customers - `admin`/`cs` belong to exactly one company
 * permanently (no permission builder), so a dropdown for them would offer a single option that
 * does nothing and imply a switch that can never happen. They keep the plain-text label instead.
 */
export function CompanySwitcher() {
  const { companies, activeCompanyId, switchCompany, user } = useAdminSession();

  if (user?.role !== "owner") {
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

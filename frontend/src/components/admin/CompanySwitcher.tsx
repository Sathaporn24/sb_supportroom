"use client";

import { useAdminSession } from "@/components/admin/AdminSessionProvider";

/**
 * Picks which customer's data the back office is showing.
 *
 * Hidden when there is nothing to choose between - a company-scoped user belongs to exactly one
 * customer, and a dropdown with a single option is just a control that does nothing.
 *
 * Unstyled on purpose - see the note on AdminBar.
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
    <label style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
      กำลังดูข้อมูลของ
      <select
        value={activeCompanyId ?? user?.companyId ?? ""}
        onChange={(event) => switchCompany(event.target.value)}
      >
        {/* An owner arrives with no company chosen; the blank option is what that state looks
            like, rather than silently showing the first customer as if they had picked it. */}
        {!activeCompanyId && <option value="">— เลือกบริษัท —</option>}
        {companies.map((company) => (
          <option key={company.id} value={company.id}>
            {company.name}
          </option>
        ))}
      </select>
    </label>
  );
}

"use client";

import { ChevronsUpDownIcon } from "lucide-react";
import Image from "next/image";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

/**
 * Sidebar header - picks which customer's data the back office is showing.
 *
 * Only `owner` ever switches between customers - `admin`/`cs` belong to exactly one company
 * permanently (no permission builder), so a dropdown for them would offer a single option that
 * does nothing and imply a switch that can never happen. They keep a plain, non-interactive
 * header instead. Either way the active company name stays visible under the logo - Figma's
 * mock only shows the logo, but dropping the name would leave no on-screen answer to "whose
 * data am I looking at right now?", which is the reason this component exists at all.
 *
 * When the sidebar is collapsed to icon-only width, the full wordmark+name block would just get
 * clipped/squished, so it's swapped for the small icon mark alone (group-data-[collapsible=icon]
 * toggles which one renders - same pattern ui/sidebar.tsx already uses everywhere else).
 */
export function CompanySwitcher() {
  const { companies, activeCompanyId, switchCompany, user } = useAdminSession();
  const activeCompany = companies.find((company) => company.id === (activeCompanyId ?? user?.companyId)) ?? companies[0];

  const logo = (
    <>
      <Image
        src="/school-bright-icon.png"
        alt="School Bright"
        width={24}
        height={24}
        priority
        className="hidden shrink-0 group-data-[collapsible=icon]:block"
      />
      <div className="flex min-w-0 flex-col items-start gap-0.5 group-data-[collapsible=icon]:hidden">
        <Image src="/school-bright-logo.png" alt="School Bright" width={137} height={24} priority />
        {activeCompany && (
          <span className="w-full truncate text-xs text-sidebar-foreground/60">{activeCompany.name}</span>
        )}
      </div>
    </>
  );

  if (user?.role !== "owner") {
    return <div className="flex w-full items-center gap-2 p-2 group-data-[collapsible=icon]:justify-center">{logo}</div>;
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className="flex w-full items-center justify-between gap-2 rounded-lg p-2 outline-none group-data-[collapsible=icon]:justify-center hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 focus-visible:ring-ring/50"
        data-testid="company-switcher-trigger"
      >
        {logo}
        <ChevronsUpDownIcon className="size-4 shrink-0 text-sidebar-foreground/50 group-data-[collapsible=icon]:hidden" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-56" data-testid="company-switcher-content">
        {companies.map((company) => (
          <DropdownMenuItem
            key={company.id}
            onClick={() => switchCompany(company.id)}
            data-testid={`company-option-${company.id}`}
          >
            {company.name}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

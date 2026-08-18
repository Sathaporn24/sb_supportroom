"use client";

import Link, { type LinkProps } from "next/link";
import { useAdminSession } from "@/components/admin/AdminSessionProvider";

type AdminLinkProps = LinkProps & Omit<React.AnchorHTMLAttributes<HTMLAnchorElement>, keyof LinkProps>;

/** Keeps the selected company in every back-office URL, including copied links and new tabs. */
export function AdminLink({ href, ...props }: AdminLinkProps) {
  const { activeCompanyId } = useAdminSession();
  const scopedHref = addCompany(href, activeCompanyId);
  return <Link href={scopedHref} {...props} />;
}

function addCompany(href: LinkProps["href"], companyId: string | null): LinkProps["href"] {
  if (!companyId) return href;

  if (typeof href === "string") {
    if (!href.startsWith("/admin") || /(?:\?|&)company=/.test(href)) return href;
    return `${href}${href.includes("?") ? "&" : "?"}company=${encodeURIComponent(companyId)}`;
  }

  const query = typeof href.query === "object" && href.query !== null ? href.query : {};
  if (!href.pathname?.toString().startsWith("/admin") || "company" in query) return href;
  return { ...href, query: { ...query, company: companyId } };
}

import { redirect } from "next/navigation";

export default async function LegacyCompaniesPage({
  searchParams,
}: {
  searchParams: Promise<{ company?: string | string[] }>;
}) {
  const { company } = await searchParams;
  const params = new URLSearchParams({ tab: "companies" });
  if (typeof company === "string") params.set("company", company);
  redirect(`/admin/settings?${params.toString()}`);
}

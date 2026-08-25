import { redirect } from "next/navigation";

export default async function LegacyAdminPage({
  searchParams,
}: {
  searchParams: Promise<{ company?: string | string[] }>;
}) {
  const { company } = await searchParams;
  const params = new URLSearchParams({ tab: "links" });
  if (typeof company === "string") params.set("company", company);
  redirect(`/admin/lessons?${params.toString()}`);
}

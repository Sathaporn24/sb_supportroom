import { redirect } from "next/navigation";

export default async function LegacyCategoriesPage({
  searchParams,
}: {
  searchParams: Promise<{ company?: string | string[] }>;
}) {
  const { company } = await searchParams;
  redirect(typeof company === "string" ? `/admin/lessons?company=${encodeURIComponent(company)}` : "/admin/lessons");
}

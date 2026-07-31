import { NextResponse } from "next/server";
import { getProviderSelection } from "@/config/env";
import { resetMockData } from "@/providers/data";
import { jsonError } from "@/lib/api-response";

export async function POST() {
  const { DATA_PROVIDER } = getProviderSelection();
  if (DATA_PROVIDER !== "mock") {
    return jsonError("CONFIG_ERROR", "Reset Demo Data ใช้ได้เฉพาะ Mock Mode เท่านั้น", 409);
  }
  resetMockData();
  return NextResponse.json({ status: "reset" });
}

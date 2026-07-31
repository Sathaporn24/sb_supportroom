import { NextResponse } from "next/server";
import { getProviderSelection } from "@/config/env";

export async function GET() {
  return NextResponse.json({
    status: "ok",
    providers: getProviderSelection(),
    timestamp: new Date().toISOString(),
  });
}

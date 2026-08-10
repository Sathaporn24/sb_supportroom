import { NextResponse } from "next/server";
import { apiError, type ApiErrorCode } from "@/types/api";
import { generateId } from "@/utils/id";

export function jsonError(code: ApiErrorCode, message: string, status: number, details?: unknown) {
  const requestId = generateId("req");
  return NextResponse.json(apiError(code, message, details, requestId), { status });
}

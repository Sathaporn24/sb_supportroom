export type ApiErrorCode =
  | "VALIDATION_ERROR"
  | "NOT_FOUND"
  | "UNAUTHORIZED"
  | "UPSTREAM_ERROR"
  | "CONFIG_ERROR"
  | "INTERNAL_ERROR";

export type ApiErrorResponse = {
  error: {
    code: ApiErrorCode;
    message: string;
    details?: unknown;
    requestId?: string;
  };
};

export function apiError(
  code: ApiErrorCode,
  message: string,
  details?: unknown,
  requestId?: string,
): ApiErrorResponse {
  return { error: { code, message, details, requestId } };
}

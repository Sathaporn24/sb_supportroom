// Must stay in step with ApiErrorCode.cs.
//
// UNAUTHORIZED and FORBIDDEN are distinct on purpose and must not be merged: UNAUTHORIZED (401)
// means "we don't know who you are", which the app answers by sending the user to sign in;
// FORBIDDEN (403) means "we know exactly who you are and the answer is no", where bouncing to a
// login screen would be a dead end - they are already signed in and signing in again changes
// nothing.
export type ApiErrorCode =
  | "VALIDATION_ERROR"
  | "NOT_FOUND"
  | "UNAUTHORIZED"
  | "FORBIDDEN"
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

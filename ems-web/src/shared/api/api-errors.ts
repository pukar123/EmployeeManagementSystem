import axios, { type AxiosError } from "axios";

export type ApiAvailabilityKind =
  | "ems_unavailable"
  | "user_management_unavailable"
  | "authentication_expired"
  | "identity_operation_pending"
  | "unknown";

export type ApiErrorSource = "ems" | "userManagement" | "unknown";

type ErrorBody = { message?: string; code?: string; title?: string };

function readErrorCode(error: AxiosError<unknown>): string | undefined {
  const data = error.response?.data;
  if (data && typeof data === "object" && data !== null && "code" in data) {
    const code = (data as ErrorBody).code;
    if (typeof code === "string") return code;
  }
  return undefined;
}

function isNetworkFailure(error: AxiosError<unknown>): boolean {
  if (error.response !== undefined) return false;
  const msg = error.message || "";
  return (
    msg === "Network Error" ||
    (error.code !== undefined && ["ERR_NETWORK", "ECONNREFUSED", "ETIMEDOUT"].includes(error.code))
  );
}

/**
 * Maps transport/API failures to independent availability states for EMS vs User Management.
 */
export function classifyApiError(error: unknown, source: ApiErrorSource): ApiAvailabilityKind {
  if (!axios.isAxiosError(error)) return "unknown";

  const ax = error as AxiosError<unknown>;
  const status = ax.response?.status;
  const code = readErrorCode(ax);

  if (code === "user_management_unavailable" || code === "identity_operation_pending") {
    return "identity_operation_pending";
  }

  if (status === 401) {
    return "authentication_expired";
  }

  if (status === 503 && source === "ems") {
    return "identity_operation_pending";
  }

  if (isNetworkFailure(ax) || status === 502 || status === 503 || status === 504) {
    if (source === "userManagement") return "user_management_unavailable";
    if (source === "ems") return "ems_unavailable";
  }

  return "unknown";
}

export function messageForAvailabilityKind(kind: ApiAvailabilityKind): string {
  switch (kind) {
    case "ems_unavailable":
      return "EMS is unavailable right now. Employee and organization data cannot be loaded. Try again shortly.";
    case "user_management_unavailable":
      return "User Management is unavailable right now. Sign-in, users, and roles cannot be reached. Try again shortly.";
    case "authentication_expired":
      return "Your session has expired. Sign in again to continue.";
    case "identity_operation_pending":
      return "This identity operation is pending because User Management is unavailable. EMS will retry when the service recovers.";
    default:
      return "Something went wrong";
  }
}

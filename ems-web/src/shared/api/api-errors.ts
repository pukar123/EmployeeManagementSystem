import axios, { type AxiosError } from "axios";

export type ApiAvailabilityKind =
  | "ems_unavailable"
  | "user_management_unavailable"
  | "authentication_expired"
  | "identity_operation_pending"
  | "rate_limited"
  | "unknown";

export type ApiErrorSource = "ems" | "userManagement" | "unknown";

type ErrorBody = { message?: string; code?: string; title?: string; retryAfterSeconds?: unknown };

/** Which page's wording to use for a rate-limit message. */
export type RateLimitAction = "sign-in" | "reset";

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

  // Checked first: a 429 is never an availability problem, whatever else the body says.
  if (status === 429) {
    return "rate_limited";
  }

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
    case "rate_limited":
      return messageForRateLimit(null);
    default:
      return "Something went wrong";
  }
}

function readRetryAfterHeader(error: AxiosError<unknown>): string | undefined {
  const headers = error.response?.headers as unknown;
  if (!headers || typeof headers !== "object") return undefined;

  const withGet = headers as { get?: (name: string) => unknown };
  if (typeof withGet.get === "function") {
    const value = withGet.get("retry-after");
    if (typeof value === "string") return value;
    if (typeof value === "number") return String(value);
  }

  for (const [name, value] of Object.entries(headers as Record<string, unknown>)) {
    if (name.toLowerCase() !== "retry-after") continue;
    if (typeof value === "string") return value;
    if (typeof value === "number") return String(value);
  }
  return undefined;
}

/**
 * How many seconds a rate-limited caller should wait, or null when the response does not say.
 * The body's `retryAfterSeconds` wins (a proxy may drop the header); then the `Retry-After` header,
 * as whole seconds or as an HTTP date.
 */
export function getRetryAfterSeconds(error: unknown, now: number = Date.now()): number | null {
  if (!axios.isAxiosError(error)) return null;
  const ax = error as AxiosError<unknown>;

  const data = ax.response?.data;
  if (data && typeof data === "object" && "retryAfterSeconds" in data) {
    const fromBody = (data as ErrorBody).retryAfterSeconds;
    if (typeof fromBody === "number" && Number.isFinite(fromBody) && fromBody > 0) {
      return Math.ceil(fromBody);
    }
  }

  const header = readRetryAfterHeader(ax)?.trim();
  if (!header) return null;

  if (/^\d+$/.test(header)) {
    const seconds = Number.parseInt(header, 10);
    return seconds > 0 ? seconds : null;
  }

  const date = Date.parse(header);
  if (Number.isNaN(date)) return null;
  const seconds = Math.ceil((date - now) / 1000);
  return seconds > 0 ? seconds : null;
}

/**
 * Friendly text for a 429. Under 90 seconds is shown in seconds, otherwise rounded up to minutes.
 * Never shows the raw status text.
 */
export function messageForRateLimit(seconds: number | null, action?: RateLimitAction): string {
  const lead =
    action === "sign-in"
      ? "Too many sign-in attempts."
      : action === "reset"
        ? "Too many password reset attempts."
        : "Too many attempts.";

  if (seconds === null || !Number.isFinite(seconds) || seconds <= 0) {
    return `${lead} Please wait a moment and try again.`;
  }

  const whole = Math.ceil(seconds);
  if (whole < 90) {
    return `${lead} Please wait about ${whole} ${whole === 1 ? "second" : "seconds"} and try again.`;
  }

  const minutes = Math.ceil(whole / 60);
  return `${lead} Please wait about ${minutes} minutes and try again.`;
}

/** The wording to use for a rate-limited request, from its URL. */
export function rateLimitActionForUrl(url: string | undefined): RateLimitAction | undefined {
  if (!url) return undefined;
  const u = url.toLowerCase();
  if (u.includes("/api/auth/login")) return "sign-in";
  if (u.includes("/api/auth/forgot-password") || u.includes("/api/auth/reset-password")) return "reset";
  return undefined;
}

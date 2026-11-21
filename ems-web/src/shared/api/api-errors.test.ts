import axios from "axios";
import { describe, expect, it } from "vitest";
import {
  classifyApiError,
  getRetryAfterSeconds,
  messageForAvailabilityKind,
  messageForRateLimit,
  rateLimitActionForUrl,
} from "@/shared/api/api-errors";
import { getErrorMessage } from "@/shared/api/http-client";

function axiosError(partial: {
  status?: number;
  code?: string;
  network?: boolean;
  message?: string;
}) {
  const config = { baseURL: "http://example", url: "/x" };
  if (partial.network) {
    return new axios.AxiosError(partial.message ?? "Network Error", "ERR_NETWORK", config as never);
  }
  return new axios.AxiosError(partial.message ?? "fail", "ERR_BAD_RESPONSE", config as never, null, {
    status: partial.status ?? 500,
    data: partial.code ? { code: partial.code, message: "x" } : { message: "x" },
    headers: {},
    statusText: "Error",
    config: config as never,
  });
}

describe("classifyApiError", () => {
  it("detects EMS unavailable on network failure", () => {
    expect(classifyApiError(axiosError({ network: true }), "ems")).toBe("ems_unavailable");
  });

  it("detects User Management unavailable on network failure", () => {
    expect(classifyApiError(axiosError({ network: true }), "userManagement")).toBe(
      "user_management_unavailable",
    );
  });

  it("detects authentication expired on 401", () => {
    expect(classifyApiError(axiosError({ status: 401 }), "ems")).toBe("authentication_expired");
  });

  it("detects identity operation pending from EMS code", () => {
    expect(
      classifyApiError(axiosError({ status: 503, code: "user_management_unavailable" }), "ems"),
    ).toBe("identity_operation_pending");
  });

  it("exposes distinct user-facing messages", () => {
    expect(messageForAvailabilityKind("ems_unavailable")).toMatch(/EMS is unavailable/i);
    expect(messageForAvailabilityKind("user_management_unavailable")).toMatch(
      /User Management is unavailable/i,
    );
    expect(messageForAvailabilityKind("authentication_expired")).toMatch(/session has expired/i);
    expect(messageForAvailabilityKind("identity_operation_pending")).toMatch(/pending/i);
  });
});

function rateLimitedError(options: {
  body?: unknown;
  headers?: Record<string, string>;
  url?: string;
}) {
  const config = { baseURL: "http://ems.test", url: options.url ?? "/api/auth/login" };
  return new axios.AxiosError("Request failed with status code 429", "ERR_BAD_REQUEST", config as never, null, {
    status: 429,
    data: options.body ?? {},
    headers: options.headers ?? {},
    statusText: "Too Many Requests",
    config: config as never,
  });
}

describe("rate limiting (B05)", () => {
  it("classifies 429 as rate_limited, before any code in the body", () => {
    expect(classifyApiError(rateLimitedError({ body: { code: "rate_limited" } }), "ems")).toBe("rate_limited");
    expect(
      classifyApiError(rateLimitedError({ body: { code: "user_management_unavailable" } }), "ems"),
    ).toBe("rate_limited");
  });

  it("reads retryAfterSeconds from the body before the header", () => {
    const error = rateLimitedError({ body: { retryAfterSeconds: 42 }, headers: { "retry-after": "300" } });
    expect(getRetryAfterSeconds(error)).toBe(42);
  });

  it("reads Retry-After as seconds when the body has no value", () => {
    expect(getRetryAfterSeconds(rateLimitedError({ headers: { "retry-after": "120" } }))).toBe(120);
  });

  it("reads Retry-After as an HTTP date", () => {
    const now = Date.parse("2026-11-21T10:00:00Z");
    const error = rateLimitedError({ headers: { "Retry-After": "Sat, 21 Nov 2026 10:02:00 GMT" } });
    expect(getRetryAfterSeconds(error, now)).toBe(120);
  });

  it("returns null when neither body nor header says how long", () => {
    expect(getRetryAfterSeconds(rateLimitedError({}))).toBeNull();
  });

  it("returns null for a non-numeric value", () => {
    expect(getRetryAfterSeconds(rateLimitedError({ body: { retryAfterSeconds: "soon" } }))).toBeNull();
    expect(getRetryAfterSeconds(rateLimitedError({ headers: { "retry-after": "soon" } }))).toBeNull();
  });

  it("shows seconds under 90 and rounds up to minutes otherwise", () => {
    expect(messageForRateLimit(30, "sign-in")).toBe(
      "Too many sign-in attempts. Please wait about 30 seconds and try again.",
    );
    expect(messageForRateLimit(120, "sign-in")).toBe(
      "Too many sign-in attempts. Please wait about 2 minutes and try again.",
    );
    expect(messageForRateLimit(91, "sign-in")).toMatch(/about 2 minutes/);
    expect(messageForRateLimit(null, "sign-in")).toBe(
      "Too many sign-in attempts. Please wait a moment and try again.",
    );
    expect(messageForRateLimit(1)).toBe("Too many attempts. Please wait about 1 second and try again.");
    expect(messageForRateLimit(600, "reset")).toBe(
      "Too many password reset attempts. Please wait about 10 minutes and try again.",
    );
  });

  it("picks the wording from the request URL", () => {
    expect(rateLimitActionForUrl("/api/auth/login")).toBe("sign-in");
    expect(rateLimitActionForUrl("/api/auth/forgot-password")).toBe("reset");
    expect(rateLimitActionForUrl("/api/auth/reset-password")).toBe("reset");
    expect(rateLimitActionForUrl("/api/invitations/accept")).toBeUndefined();
  });

  it("getErrorMessage gives the friendly text, never the raw status text", () => {
    const signIn = rateLimitedError({ body: { code: "rate_limited", message: "Too many attempts.", retryAfterSeconds: 120 } });
    expect(getErrorMessage(signIn)).toBe("Too many sign-in attempts. Please wait about 2 minutes and try again.");

    const forgot = rateLimitedError({ url: "/api/auth/forgot-password", headers: { "retry-after": "45" } });
    expect(getErrorMessage(forgot)).toBe("Too many password reset attempts. Please wait about 45 seconds and try again.");

    expect(getErrorMessage(rateLimitedError({}))).not.toMatch(/status code|Request failed/i);
  });

  it("has a generic availability message for rate_limited", () => {
    expect(messageForAvailabilityKind("rate_limited")).toBe(
      "Too many attempts. Please wait a moment and try again.",
    );
  });
});

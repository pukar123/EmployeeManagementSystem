import axios from "axios";
import { describe, expect, it } from "vitest";
import { classifyApiError, messageForAvailabilityKind } from "@/shared/api/api-errors";

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

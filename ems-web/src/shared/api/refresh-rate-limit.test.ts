import axios, { AxiosError } from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/shared/auth/auth-storage", () => ({
  saveAuthResponse: vi.fn(),
  clearAuth: vi.fn(),
  getAccessToken: vi.fn(() => "access-token"),
  getRefreshToken: vi.fn(() => "refresh-token"),
  setAuthChangeHandler: vi.fn(),
}));

import { clearAuth } from "@/shared/auth/auth-storage";
import { emsHttpClient, getApiAvailabilityKind, refreshAccessToken } from "@/shared/api/http-client";

function refreshError(status: number) {
  const config = { url: "http://ems.test/api/auth/refresh", headers: {} };
  return new AxiosError(`Request failed with status code ${status}`, "ERR_BAD_REQUEST", config as never, null, {
    status,
    data: status === 429 ? { code: "rate_limited", message: "Too many attempts.", retryAfterSeconds: 30 } : {},
    headers: status === 429 ? { "retry-after": "30" } : {},
    statusText: status === 429 ? "Too Many Requests" : "Bad Request",
    config: config as never,
  });
}

describe("token refresh when rate limited (B05)", () => {
  beforeEach(() => {
    vi.mocked(clearAuth).mockClear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("keeps the session when the refresh gets 429", async () => {
    vi.spyOn(axios, "post").mockRejectedValue(refreshError(429));

    const token = await refreshAccessToken();

    expect(token).toBeNull();
    expect(clearAuth).not.toHaveBeenCalled();
  });

  it("still clears the session when the refresh is refused for another reason", async () => {
    vi.spyOn(axios, "post").mockRejectedValue(refreshError(400));

    const token = await refreshAccessToken();

    expect(token).toBeNull();
    expect(clearAuth).toHaveBeenCalledTimes(1);
  });

  it("fails the original request with the rate-limit error, not a session-expired error", async () => {
    vi.spyOn(axios, "post").mockRejectedValue(refreshError(429));
    emsHttpClient.defaults.adapter = async (config) => {
      throw new AxiosError("Unauthorized", "ERR_BAD_REQUEST", config, null, {
        status: 401,
        data: {},
        headers: {},
        statusText: "Unauthorized",
        config,
      });
    };

    const failure = await emsHttpClient.get("/api/Employees").catch((error: unknown) => error);

    expect(getApiAvailabilityKind(failure)).toBe("rate_limited");
    expect(clearAuth).not.toHaveBeenCalled();
  });
});

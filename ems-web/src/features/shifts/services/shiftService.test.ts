import type { AxiosAdapter, InternalAxiosRequestConfig } from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/shared/auth/auth-storage", () => ({
  getAccessToken: vi.fn(() => "access-token"),
}));

import { emsHttpClient } from "@/shared/api/http-client";
import { shiftService } from "./shiftService";

const EMS_ORIGIN = "http://ems.test";

type Captured = { method: string; url: string; data?: unknown; params?: unknown };

function installCapture(client: typeof emsHttpClient, bucket: Captured[]): void {
  const adapter: AxiosAdapter = async (config: InternalAxiosRequestConfig) => {
    const method = (config.method ?? "get").toUpperCase();
    const baseURL = config.baseURL ?? "";
    const path = config.url ?? "";
    const url = `${baseURL.replace(/\/$/, "")}${path.startsWith("/") ? path : `/${path}`}`;
    bucket.push({ method, url, data: config.data, params: config.params });
    return {
      data: {},
      status: 200,
      statusText: "OK",
      headers: {},
      config,
    };
  };
  client.defaults.adapter = adapter;
}

describe("shiftService", () => {
  const requests: Captured[] = [];

  beforeEach(() => {
    requests.length = 0;
    installCapture(emsHttpClient, requests);
  });

  it("routes list requests to EMS with query params", async () => {
    await shiftService.getAll({ organizationId: 1, employeeId: 5 });

    expect(requests).toHaveLength(1);
    expect(requests[0]).toMatchObject({
      method: "GET",
      url: `${EMS_ORIGIN}/api/Shifts`,
      params: { organizationId: 1, employeeId: 5 },
    });
  });

  it("routes create, update, and delete to EMS", async () => {
    await shiftService.createShift({
      organizationId: 1,
      employeeId: 5,
      title: "Morning shift",
      startAtUtc: "2026-07-10T00:00:00.000Z",
      endAtUtc: "2026-07-10T08:00:00.000Z",
    });
    await shiftService.updateShift(3, {
      title: "Updated shift",
      startAtUtc: "2026-07-10T00:00:00.000Z",
      endAtUtc: "2026-07-10T08:00:00.000Z",
      status: 0,
    });
    await shiftService.deleteShift(3);

    expect(requests.map((r) => ({ method: r.method, url: r.url }))).toEqual([
      { method: "POST", url: `${EMS_ORIGIN}/api/Shifts` },
      { method: "PUT", url: `${EMS_ORIGIN}/api/Shifts/3` },
      { method: "DELETE", url: `${EMS_ORIGIN}/api/Shifts/3` },
    ]);
  });
});

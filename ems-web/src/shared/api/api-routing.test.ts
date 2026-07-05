import type { AxiosAdapter, InternalAxiosRequestConfig } from "axios";
import axios, { AxiosError } from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/shared/auth/auth-storage", () => ({
  saveAuthResponse: vi.fn(),
  clearAuth: vi.fn(),
  getAccessToken: vi.fn(() => "access-token"),
  getRefreshToken: vi.fn(() => "refresh-token"),
  setAuthChangeHandler: vi.fn(),
}));

import { authService } from "@/features/auth/services/authService";
import {
  adminSetPassword,
  createRole,
  createUser,
  fetchRoles,
  fetchUserRoles,
  fetchUsers,
  setUserRoles,
  updateRole,
  updateUser,
} from "@/features/user-management/services/userManagementApi";
import { emsHttpClient, userManagementHttpClient } from "@/shared/api/http-client";

const EMS_ORIGIN = "http://ems.test";
const UM_ORIGIN = "http://um.test";

type Captured = { method: string; url: string };

function installCapture(client: typeof emsHttpClient, bucket: Captured[]): void {
  const adapter: AxiosAdapter = async (config: InternalAxiosRequestConfig) => {
    const method = (config.method ?? "get").toUpperCase();
    const baseURL = config.baseURL ?? "";
    const path = config.url ?? "";
    const url = `${baseURL.replace(/\/$/, "")}${path.startsWith("/") ? path : `/${path}`}`;
    bucket.push({ method, url });
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

describe("dual API routing", () => {
  const emsRequests: Captured[] = [];
  const umRequests: Captured[] = [];

  beforeEach(() => {
    emsRequests.length = 0;
    umRequests.length = 0;
    installCapture(emsHttpClient, emsRequests);
    installCapture(userManagementHttpClient, umRequests);
  });

  it("sends authentication, password, and invitation acceptance only to User Management", async () => {
    await authService.login("a@b.com", "secret");
    await authService.changePassword("old", "new-password");
    await authService.acceptInvitation("invite-token", "new-password");

    expect(emsRequests).toEqual([]);
    expect(umRequests.map((r) => r.url)).toEqual([
      `${UM_ORIGIN}/api/auth/login`,
      `${UM_ORIGIN}/api/auth/change-password`,
      `${UM_ORIGIN}/api/invitations/accept`,
    ]);
  });

  it("sends user and role administration only to User Management", async () => {
    await fetchUsers();
    await createUser({ email: "u@x.com", password: "pw", isActive: true });
    await updateUser(1, { email: "u@x.com", isActive: true });
    await adminSetPassword(1, { newPassword: "pw2" });
    await fetchUserRoles(1);
    await setUserRoles(1, [2]);
    await fetchRoles();
    await createRole({ name: "Manager" });
    await updateRole(2, { name: "Manager" });

    expect(emsRequests).toEqual([]);
    for (const req of umRequests) {
      expect(req.url.startsWith(UM_ORIGIN)).toBe(true);
      expect(req.url.startsWith(EMS_ORIGIN)).toBe(false);
    }
    expect(umRequests.map((r) => r.url)).toEqual([
      `${UM_ORIGIN}/api/Users`,
      `${UM_ORIGIN}/api/Users`,
      `${UM_ORIGIN}/api/Users/1`,
      `${UM_ORIGIN}/api/Users/1/password`,
      `${UM_ORIGIN}/api/Users/1/roles`,
      `${UM_ORIGIN}/api/Users/1/roles`,
      `${UM_ORIGIN}/api/Roles`,
      `${UM_ORIGIN}/api/Roles`,
      `${UM_ORIGIN}/api/Roles/2`,
    ]);
  });

  it("never routes auth, users, roles, password, or invitation accept paths through EMS.API", async () => {
    const forbiddenOnEms = [
      "/api/auth/login",
      "/api/auth/refresh",
      "/api/auth/revoke",
      "/api/auth/change-password",
      "/api/Users",
      "/api/Roles",
      "/api/invitations/accept",
    ];

    await authService.login("a@b.com", "secret");
    await authService.changePassword("old", "new");
    await authService.acceptInvitation("t", "password1");
    await fetchUsers();
    await fetchRoles();
    await adminSetPassword(9, { newPassword: "x" });

    expect(emsRequests).toHaveLength(0);
    for (const req of emsRequests) {
      for (const path of forbiddenOnEms) {
        expect(req.url.toLowerCase().includes(path.toLowerCase())).toBe(false);
      }
    }
  });

  it("refreshes via User Management then retries the failed request on the original EMS client", async () => {
    let emsAttempts = 0;
    const refreshCalls: string[] = [];

    vi.spyOn(axios, "post").mockImplementation(async (url: string) => {
      refreshCalls.push(String(url));
      return {
        data: {
          accessToken: "new-access",
          refreshToken: "new-refresh",
          user: { id: 1, email: "a@b.com", userName: "a" },
          mustChangePassword: false,
        },
        status: 200,
        statusText: "OK",
        headers: {},
        config: {},
      } as never;
    });

    emsHttpClient.defaults.adapter = async (config) => {
      emsAttempts += 1;
      const url = `${config.baseURL ?? ""}${config.url ?? ""}`;
      expect(url.startsWith(EMS_ORIGIN)).toBe(true);
      if (emsAttempts === 1) {
        throw new AxiosError("Unauthorized", "ERR_BAD_REQUEST", config, null, {
          status: 401,
          data: {},
          headers: {},
          statusText: "Unauthorized",
          config,
        });
      }
      return {
        data: { ok: true },
        status: 200,
        statusText: "OK",
        headers: {},
        config,
      };
    };

    const { data } = await emsHttpClient.get("/api/Employees");
    expect(data).toEqual({ ok: true });
    expect(emsAttempts).toBe(2);
    expect(refreshCalls.some((u) => u.includes(`${UM_ORIGIN}/api/auth/refresh`))).toBe(true);
  });
});

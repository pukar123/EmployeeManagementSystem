import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from "axios";
import type { AuthResponse } from "@/shared/auth/auth-types";
import { clearAuth, getAccessToken, getRefreshToken, saveAuthResponse } from "@/shared/auth/auth-storage";
import {
  type ApiAvailabilityKind,
  classifyApiError,
  messageForAvailabilityKind,
} from "@/shared/api/api-errors";

/** Prefer IPv4 loopback so Windows does not resolve `localhost` to `::1` while Kestrel listens on IPv4 only. */
export function normalizeApiOrigin(raw: string): string {
  const t = raw.trim().replace(/\/$/, "");
  if (!t) return "";
  try {
    const u = new URL(t);
    if (u.hostname === "localhost") {
      u.hostname = "127.0.0.1";
    }
    return u.origin;
  } catch {
    return t;
  }
}

function readPublicOrigin(...keys: string[]): string {
  for (const key of keys) {
    const value = process.env[key];
    if (value && value.trim()) return normalizeApiOrigin(value);
  }
  return "";
}

/** EMS.API origin (employees, org, attendance, menus, employee↔user orchestration). */
export const emsApiBaseUrl = readPublicOrigin(
  "NEXT_PUBLIC_EMS_API_BASE_URL",
  // Legacy alias — remove after all environments migrate.
  "NEXT_PUBLIC_API_BASE_URL",
);

/** Pukar.Usermanagement.Host origin (auth, users, roles, invitation acceptance). */
export const userManagementApiBaseUrl = readPublicOrigin(
  "NEXT_PUBLIC_USER_MANAGEMENT_API_BASE_URL",
  // Legacy alias — remove after all environments migrate.
  "NEXT_PUBLIC_UM_API_BASE_URL",
);

export const emsHttpClient: AxiosInstance = axios.create({
  baseURL: emsApiBaseUrl,
  headers: { "Content-Type": "application/json" },
});

export const userManagementHttpClient: AxiosInstance = axios.create({
  baseURL: userManagementApiBaseUrl,
  headers: { "Content-Type": "application/json" },
});

type ConfigWithRetry = InternalAxiosRequestConfig & { _retry?: boolean };

let refreshInFlight: Promise<string | null> | null = null;

function isAuthRequestUrl(url: string | undefined): boolean {
  if (!url) return false;
  const u = url.toLowerCase();
  return (
    u.includes("/api/auth/login") ||
    u.includes("/api/auth/register") ||
    u.includes("/api/auth/refresh") ||
    u.includes("/api/auth/revoke") ||
    u.includes("/api/invitations/accept")
  );
}

/**
 * Renews the access token via User Management only (never EMS.API).
 * Uses bare axios so refresh is not intercepted by either client.
 */
export async function refreshAccessToken(): Promise<string | null> {
  if (refreshInFlight) return refreshInFlight;
  const rt = getRefreshToken();
  if (!rt) return null;
  const base = userManagementApiBaseUrl.replace(/\/$/, "");
  if (!base) return null;

  refreshInFlight = (async () => {
    try {
      const { data } = await axios.post<AuthResponse>(
        `${base}/api/auth/refresh`,
        { refreshToken: rt },
        { headers: { "Content-Type": "application/json" } },
      );
      saveAuthResponse(data);
      return data.accessToken;
    } catch {
      clearAuth();
      if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login")) {
        window.location.assign("/login?reason=expired");
      }
      return null;
    } finally {
      refreshInFlight = null;
    }
  })();
  return refreshInFlight;
}

/**
 * Attach Bearer tokens and 401→refresh→retry on the given client.
 * Refresh always hits User Management; the failed request is retried on `client`
 * (EMS or User Management), preserving the original API boundary.
 */
function attachAuthInterceptors(client: AxiosInstance) {
  client.interceptors.request.use((config) => {
    if (isAuthRequestUrl(config.url)) return config;
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  client.interceptors.response.use(
    (response) => response,
    async (error: unknown) => {
      if (!axios.isAxiosError(error) || !error.config) return Promise.reject(error);
      const status = error.response?.status;
      const config = error.config as ConfigWithRetry;
      if (status !== 401 || config._retry || isAuthRequestUrl(config.url)) {
        return Promise.reject(error);
      }
      config._retry = true;
      const newToken = await refreshAccessToken();
      if (!newToken) return Promise.reject(error);
      config.headers.Authorization = `Bearer ${newToken}`;
      return client(config);
    },
  );
}

attachAuthInterceptors(emsHttpClient);
attachAuthInterceptors(userManagementHttpClient);

export type ApiErrorBody = { message?: string; title?: string; code?: string };

/** POST multipart to EMS.API (e.g. file upload). Avoids axios default JSON Content-Type on FormData. */
export async function postFormData<T>(urlPath: string, formData: FormData): Promise<T> {
  const base = emsApiBaseUrl.replace(/\/$/, "");
  const path = urlPath.startsWith("/") ? urlPath : `/${urlPath}`;
  const headers: HeadersInit = {};
  const token = getAccessToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }
  const res = await fetch(`${base}${path}`, { method: "POST", body: formData, headers });
  if (!res.ok) {
    const text = await res.text();
    let message = `Request failed (${res.status})`;
    try {
      const body = JSON.parse(text) as unknown;
      if (body && typeof body === "object" && body !== null && "message" in body) {
        const m = (body as ApiErrorBody).message;
        if (typeof m === "string") message = m;
      }
    } catch {
      if (text.length > 0 && text.length < 500) message = text;
    }
    throw new Error(message);
  }
  return res.json() as Promise<T>;
}

export type ApiErrorSource = "ems" | "userManagement" | "unknown";

export function resolveErrorSource(error: unknown): ApiErrorSource {
  if (!axios.isAxiosError(error)) return "unknown";
  const base = error.config?.baseURL ?? "";
  if (base && emsApiBaseUrl && base.replace(/\/$/, "") === emsApiBaseUrl.replace(/\/$/, "")) {
    return "ems";
  }
  if (
    base &&
    userManagementApiBaseUrl &&
    base.replace(/\/$/, "") === userManagementApiBaseUrl.replace(/\/$/, "")
  ) {
    return "userManagement";
  }
  const url = `${base}${error.config?.url ?? ""}`;
  if (userManagementApiBaseUrl && url.startsWith(userManagementApiBaseUrl)) return "userManagement";
  if (emsApiBaseUrl && url.startsWith(emsApiBaseUrl)) return "ems";
  return "unknown";
}

export function getApiAvailabilityKind(error: unknown): ApiAvailabilityKind {
  return classifyApiError(error, resolveErrorSource(error));
}

export function getErrorMessage(error: unknown): string {
  const kind = getApiAvailabilityKind(error);
  if (kind !== "unknown") {
    return messageForAvailabilityKind(kind);
  }

  if (axios.isAxiosError(error)) {
    const ax = error as AxiosError<unknown>;
    const data = ax.response?.data;
    if (typeof data === "string" && data.length > 0) return data;
    if (data && typeof data === "object" && data !== null && "message" in data) {
      const m = (data as ApiErrorBody).message;
      if (typeof m === "string") return m;
    }
    return ax.message || "Request failed";
  }
  if (error instanceof Error) return error.message;
  return "Something went wrong";
}

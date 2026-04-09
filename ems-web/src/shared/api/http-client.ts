import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { AuthResponse } from "@/shared/auth/auth-types";
import { clearAuth, getAccessToken, getRefreshToken, saveAuthResponse } from "@/shared/auth/auth-storage";

export const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

export const httpClient = axios.create({
  baseURL: apiBaseUrl,
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
    u.includes("/api/auth/revoke")
  );
}

async function refreshAccessToken(): Promise<string | null> {
  if (refreshInFlight) return refreshInFlight;
  const rt = getRefreshToken();
  if (!rt) return null;
  const base = apiBaseUrl.replace(/\/$/, "");
  refreshInFlight = (async () => {
    try {
      const { data } = await axios.post<AuthResponse>(`${base}/api/auth/refresh`, { refreshToken: rt }, {
        headers: { "Content-Type": "application/json" },
      });
      saveAuthResponse(data);
      return data.accessToken;
    } catch {
      clearAuth();
      if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login")) {
        window.location.assign("/login");
      }
      return null;
    } finally {
      refreshInFlight = null;
    }
  })();
  return refreshInFlight;
}

httpClient.interceptors.request.use((config) => {
  if (isAuthRequestUrl(config.url)) return config;
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

httpClient.interceptors.response.use(
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
    return httpClient(config);
  },
);

export type ApiErrorBody = { message?: string; title?: string };

/** POST multipart (e.g. file upload). Avoids axios default JSON Content-Type on FormData. */
export async function postFormData<T>(urlPath: string, formData: FormData): Promise<T> {
  const base = apiBaseUrl.replace(/\/$/, "");
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

export function getErrorMessage(error: unknown): string {
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

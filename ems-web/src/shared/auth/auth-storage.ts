import type { AuthResponse, AuthUser } from "./auth-types";

let onAuthChanged: (() => void) | undefined;

/** Called by AuthProvider so token refresh updates React state. */
export function setAuthChangeHandler(handler: (() => void) | undefined): void {
  onAuthChanged = handler;
}

const ACCESS = "ems_access_token";
const REFRESH = "ems_refresh_token";
const USER = "ems_user";

export function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(ACCESS);
}

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(REFRESH);
}

export function getStoredUser(): AuthUser | null {
  if (typeof window === "undefined") return null;
  const raw = window.localStorage.getItem(USER);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function saveAuthResponse(data: AuthResponse): void {
  window.localStorage.setItem(ACCESS, data.accessToken);
  window.localStorage.setItem(REFRESH, data.refreshToken);
  window.localStorage.setItem(USER, JSON.stringify(data.user));
  onAuthChanged?.();
}

export function clearAuth(): void {
  window.localStorage.removeItem(ACCESS);
  window.localStorage.removeItem(REFRESH);
  window.localStorage.removeItem(USER);
  onAuthChanged?.();
}

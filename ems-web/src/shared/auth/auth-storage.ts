import type { AuthResponse, AuthUser } from "./auth-types";

let onAuthChanged: (() => void) | undefined;

/** Called by AuthProvider so token refresh updates React state. */
export function setAuthChangeHandler(handler: (() => void) | undefined): void {
  onAuthChanged = handler;
}

const ACCESS = "ems_access_token";
const REFRESH = "ems_refresh_token";
const USER = "ems_user";
const MUST_CHANGE_PASSWORD = "ems_must_change_password";

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
  window.localStorage.setItem(MUST_CHANGE_PASSWORD, JSON.stringify(Boolean(data.mustChangePassword)));
  onAuthChanged?.();
}

export function getMustChangePassword(): boolean {
  if (typeof window === "undefined") return false;
  return window.localStorage.getItem(MUST_CHANGE_PASSWORD) === "true";
}

export function setMustChangePassword(value: boolean): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(MUST_CHANGE_PASSWORD, JSON.stringify(value));
  onAuthChanged?.();
}

export function clearAuth(): void {
  window.localStorage.removeItem(ACCESS);
  window.localStorage.removeItem(REFRESH);
  window.localStorage.removeItem(USER);
  window.localStorage.removeItem(MUST_CHANGE_PASSWORD);
  onAuthChanged?.();
}

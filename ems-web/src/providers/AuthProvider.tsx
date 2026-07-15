"use client";

import { createContext, useCallback, useContext, useMemo, useSyncExternalStore, type ReactNode } from "react";
import { authService } from "@/features/auth/services/authService";
import type { AuthUser } from "@/shared/auth/auth-types";
import {
  getAccessToken,
  getMustChangePassword,
  getStoredUser,
  notifyAuthStorageChanged,
  setAuthChangeHandler,
  setMustChangePassword,
} from "@/shared/auth/auth-storage";

export type AuthContextValue = {
  isReady: boolean;
  isAuthenticated: boolean;
  mustChangePassword: boolean;
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  completePasswordChange: (currentPassword: string, newPassword: string) => Promise<void>;
  /** Rehydrate from localStorage after external updates (e.g. refresh interceptor). */
  syncFromStorage: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

type AuthSnapshot = { user: AuthUser | null; mustChangePassword: boolean };

const unauthenticatedSnapshot: AuthSnapshot = { user: null, mustChangePassword: false };
let cachedSnapshotKey: string | undefined;
let cachedSnapshot: AuthSnapshot = unauthenticatedSnapshot;

function readAuthFromStorage(): AuthSnapshot {
  if (typeof window === "undefined") {
    return unauthenticatedSnapshot;
  }

  const token = getAccessToken();
  if (!token) {
    cachedSnapshotKey = undefined;
    cachedSnapshot = unauthenticatedSnapshot;
    return cachedSnapshot;
  }

  const user = getStoredUser();
  const mustChangePassword = getMustChangePassword();
  const snapshotKey = JSON.stringify({ token, user, mustChangePassword });

  if (snapshotKey === cachedSnapshotKey) {
    return cachedSnapshot;
  }

  cachedSnapshotKey = snapshotKey;
  cachedSnapshot = { user, mustChangePassword };
  return cachedSnapshot;
}

const serverSnapshot = unauthenticatedSnapshot;
const subscribeToHydration = () => () => {};
const clientReadySnapshot = () => true;
const serverReadySnapshot = () => false;

export function AuthProvider({ children }: { children: ReactNode }) {
  const subscribe = useCallback((onStoreChange: () => void) => {
    setAuthChangeHandler(onStoreChange);
    return () => setAuthChangeHandler(undefined);
  }, []);

  const snapshot = useSyncExternalStore(
    subscribe,
    readAuthFromStorage,
    () => serverSnapshot,
  );

  const isReady = useSyncExternalStore(
    subscribeToHydration,
    clientReadySnapshot,
    serverReadySnapshot,
  );
  const { user, mustChangePassword } = snapshot;

  const syncFromStorage = useCallback(() => {
    notifyAuthStorageChanged();
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    await authService.login(email, password);
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
  }, []);

  const completePasswordChange = useCallback(async (currentPassword: string, newPassword: string) => {
    await authService.changePassword(currentPassword, newPassword);
    setMustChangePassword(false);
  }, []);

  const isAuthenticated = user !== null;

  const value = useMemo<AuthContextValue>(
    () => ({
      isReady,
      isAuthenticated,
      mustChangePassword,
      user,
      login,
      logout,
      completePasswordChange,
      syncFromStorage,
    }),
    [isReady, isAuthenticated, mustChangePassword, user, login, logout, completePasswordChange, syncFromStorage],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
}

"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { authService } from "@/features/auth/services/authService";
import type { AuthUser } from "@/shared/auth/auth-types";
import {
  getAccessToken,
  getMustChangePassword,
  getStoredUser,
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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isReady, setIsReady] = useState(false);
  const [user, setUser] = useState<AuthUser | null>(null);
  const [mustChangePassword, setMustChangePasswordState] = useState(false);

  const syncFromStorage = useCallback(() => {
    const token = getAccessToken();
    setUser(token ? getStoredUser() : null);
    setMustChangePasswordState(token ? getMustChangePassword() : false);
  }, []);

  useEffect(() => {
    syncFromStorage();
    setIsReady(true);
  }, [syncFromStorage]);

  useEffect(() => {
    setAuthChangeHandler(() => {
      const token = getAccessToken();
      setUser(token ? getStoredUser() : null);
      setMustChangePasswordState(token ? getMustChangePassword() : false);
    });
    return () => setAuthChangeHandler(undefined);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const data = await authService.login(email, password);
    setUser(data.user);
    setMustChangePasswordState(data.mustChangePassword);
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
    setUser(null);
    setMustChangePasswordState(false);
  }, []);

  const completePasswordChange = useCallback(async (currentPassword: string, newPassword: string) => {
    await authService.changePassword(currentPassword, newPassword);
    setMustChangePassword(false);
    setMustChangePasswordState(false);
  }, []);

  const isAuthenticated = user !== null && Boolean(getAccessToken());

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

"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { authService } from "@/features/auth/services/authService";
import type { AuthUser } from "@/shared/auth/auth-types";
import { getStoredUser, setAuthChangeHandler } from "@/shared/auth/auth-storage";

export type AuthContextValue = {
  isReady: boolean;
  isAuthenticated: boolean;
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  /** Rehydrate from localStorage after external updates (e.g. refresh interceptor). */
  syncFromStorage: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isReady, setIsReady] = useState(false);
  const [user, setUser] = useState<AuthUser | null>(null);

  const syncFromStorage = useCallback(() => {
    setUser(getStoredUser());
  }, []);

  useEffect(() => {
    syncFromStorage();
    setIsReady(true);
  }, [syncFromStorage]);

  useEffect(() => {
    setAuthChangeHandler(() => {
      setUser(getStoredUser());
    });
    return () => setAuthChangeHandler(undefined);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const data = await authService.login(email, password);
    setUser(data.user);
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
    setUser(null);
  }, []);

  const isAuthenticated = user !== null;

  const value = useMemo<AuthContextValue>(
    () => ({
      isReady,
      isAuthenticated,
      user,
      login,
      logout,
      syncFromStorage,
    }),
    [isReady, isAuthenticated, user, login, logout, syncFromStorage],
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

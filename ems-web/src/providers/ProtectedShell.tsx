"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { OrganizationGate } from "@/providers/OrganizationGate";
import { OrganizationProvider } from "@/providers/OrganizationProvider";
import { useAuth } from "@/providers/AuthProvider";
import { EmployeePortalShell } from "@/shared/components/layout/EmployeePortalShell";
import { EmsTailAdminShell } from "@/shared/components/layout/EmsTailAdminShell";
import { Spinner } from "@/shared/components/Spinner";

const LOGIN_PATH = "/login";
const CHANGE_PASSWORD_PATH = "/change-password";

export function ProtectedShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { isReady, isAuthenticated, mustChangePassword } = useAuth();

  useEffect(() => {
    if (!isReady) return;
    if (pathname === LOGIN_PATH && isAuthenticated) {
      router.replace(mustChangePassword ? CHANGE_PASSWORD_PATH : "/");
    }
  }, [isReady, pathname, isAuthenticated, mustChangePassword, router]);

  useEffect(() => {
    if (!isReady) return;
    if (pathname === LOGIN_PATH || pathname === CHANGE_PASSWORD_PATH) return;
    if (!isAuthenticated) {
      router.replace(LOGIN_PATH);
    }
  }, [isReady, pathname, isAuthenticated, router]);

  useEffect(() => {
    if (!isReady || !isAuthenticated) return;
    if (mustChangePassword && pathname !== CHANGE_PASSWORD_PATH) {
      router.replace(CHANGE_PASSWORD_PATH);
      return;
    }
    if (!mustChangePassword && pathname === CHANGE_PASSWORD_PATH) {
      router.replace("/");
    }
  }, [isReady, isAuthenticated, mustChangePassword, pathname, router]);

  if (!isReady) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
        <Spinner />
        <p className="text-sm text-zinc-600 dark:text-zinc-400">Loading…</p>
      </div>
    );
  }

  if (pathname === LOGIN_PATH) {
    if (isAuthenticated) {
      return (
        <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
          <Spinner />
          <p className="text-sm text-zinc-600 dark:text-zinc-400">Redirecting…</p>
        </div>
      );
    }
    return <>{children}</>;
  }

  if (pathname === CHANGE_PASSWORD_PATH) {
    if (!isAuthenticated) {
      return (
        <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
          <Spinner />
          <p className="text-sm text-zinc-600 dark:text-zinc-400">Redirecting to sign in…</p>
        </div>
      );
    }
    if (!mustChangePassword) {
      return (
        <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
          <Spinner />
          <p className="text-sm text-zinc-600 dark:text-zinc-400">Redirecting…</p>
        </div>
      );
    }
    return <>{children}</>;
  }

  if (!isAuthenticated) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
        <Spinner />
        <p className="text-sm text-zinc-600 dark:text-zinc-400">Redirecting to sign in…</p>
      </div>
    );
  }

  const isEmployeePortal = pathname.startsWith("/employee-portal");

  return (
    <OrganizationProvider>
      <OrganizationGate>
        {isEmployeePortal ? (
          <EmployeePortalShell>{children}</EmployeePortalShell>
        ) : (
          <EmsTailAdminShell>{children}</EmsTailAdminShell>
        )}
      </OrganizationGate>
    </OrganizationProvider>
  );
}

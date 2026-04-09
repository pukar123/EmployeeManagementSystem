"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { OrganizationGate } from "@/providers/OrganizationGate";
import { OrganizationProvider } from "@/providers/OrganizationProvider";
import { useAuth } from "@/providers/AuthProvider";
import { AppNav } from "@/shared/components/AppNav";
import { Spinner } from "@/shared/components/Spinner";

const LOGIN_PATH = "/login";

export function ProtectedShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { isReady, isAuthenticated } = useAuth();

  useEffect(() => {
    if (!isReady) return;
    if (pathname === LOGIN_PATH && isAuthenticated) {
      router.replace("/");
    }
  }, [isReady, pathname, isAuthenticated, router]);

  useEffect(() => {
    if (!isReady) return;
    if (pathname === LOGIN_PATH) return;
    if (!isAuthenticated) {
      router.replace(LOGIN_PATH);
    }
  }, [isReady, pathname, isAuthenticated, router]);

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

  if (!isAuthenticated) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
        <Spinner />
        <p className="text-sm text-zinc-600 dark:text-zinc-400">Redirecting to sign in…</p>
      </div>
    );
  }

  return (
    <OrganizationProvider>
      <OrganizationGate>
        <AppNav />
        {children}
      </OrganizationGate>
    </OrganizationProvider>
  );
}

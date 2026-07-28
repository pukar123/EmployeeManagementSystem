"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { ApiAvailabilityAlert } from "@/shared/components/ApiAvailabilityAlert";
import { Spinner } from "@/shared/components/Spinner";
import { useOrganizationContext } from "./OrganizationProvider";

const SETUP_CREATE_PATH = "/setup";
const ORG_UPDATE_PATH = "/organization/setup";

/** Routes reachable before an organization exists (home shows create vs update links). */
const allowedPathsWhenNeedsSetup = new Set<string>(["/", SETUP_CREATE_PATH]);

function isAllowedWhenNeedsSetup(pathname: string): boolean {
  return (
    allowedPathsWhenNeedsSetup.has(pathname) ||
    pathname === "/employee-portal" ||
    pathname.startsWith("/employee-portal/")
  );
}

export function OrganizationGate({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { needsSetup, isLoading, isError, error } = useOrganizationContext();

  useEffect(() => {
    if (isLoading) return;

    if (needsSetup) {
      if (pathname === ORG_UPDATE_PATH) {
        router.replace("/");
        return;
      }
      if (!isAllowedWhenNeedsSetup(pathname)) {
        router.replace("/");
      }
      return;
    }

    if (!needsSetup && pathname === SETUP_CREATE_PATH) {
      router.replace("/");
    }
  }, [needsSetup, isLoading, pathname, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
        <Spinner />
        <p className="text-sm text-muted-foreground">Loading organization…</p>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="mx-auto max-w-lg px-4 py-16">
        <ApiAvailabilityAlert error={error} />
        <p className="mt-3 text-sm text-muted-foreground">
          Please try again in a moment. If this keeps happening, contact your administrator.
        </p>
      </div>
    );
  }

  if (needsSetup && !isAllowedWhenNeedsSetup(pathname)) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
        <Spinner />
        <p className="text-sm text-muted-foreground">Redirecting…</p>
      </div>
    );
  }

  if (!needsSetup && pathname === SETUP_CREATE_PATH) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center gap-3 px-4">
        <Spinner />
        <p className="text-sm text-muted-foreground">Redirecting…</p>
      </div>
    );
  }

  return <>{children}</>;
}

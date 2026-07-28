"use client";

import Link from "next/link";
import { useOrganizationContext } from "@/providers/OrganizationProvider";

const linkClass =
  "inline-flex items-center rounded-xl border border-input bg-background px-4 py-2 text-sm font-medium text-foreground shadow-soft transition hover:border-primary/40 hover:bg-muted/60";

export function OrganizationSetupLinks() {
  const { needsSetup, isLoading, isError, currentOrganization } = useOrganizationContext();

  if (isLoading || isError) {
    return null;
  }

  if (needsSetup) {
    return (
      <p className="mt-4">
        <Link href="/setup" className={linkClass}>
          Create organization
        </Link>
        <span className="ml-3 text-sm text-muted-foreground">Required before managing employees and related data.</span>
      </p>
    );
  }

  if (!currentOrganization) {
    return null;
  }

  return (
    <p className="mt-4">
      <Link href="/organization/setup" className={linkClass}>
        Update organization
      </Link>
      <span className="ml-3 text-sm text-muted-foreground">
        Current: <span className="font-medium text-muted-foreground">{currentOrganization.name}</span>
      </span>
    </p>
  );
}

"use client";

import { getApiAvailabilityKind, getErrorMessage } from "@/shared/api/http-client";
import type { ApiAvailabilityKind } from "@/shared/api/api-errors";

const TITLES: Record<Exclude<ApiAvailabilityKind, "unknown">, string> = {
  ems_unavailable: "EMS unavailable",
  user_management_unavailable: "User Management unavailable",
  authentication_expired: "Authentication expired",
  identity_operation_pending: "Identity operation pending",
};

type Props = {
  error: unknown;
  className?: string;
};

/** Renders a dedicated banner for known dual-API failure modes. */
export function ApiAvailabilityAlert({ error, className }: Props) {
  const kind = getApiAvailabilityKind(error);
  const message = getErrorMessage(error);
  const title = kind === "unknown" ? "Request failed" : TITLES[kind];

  return (
    <div
      role="alert"
      className={
        className ??
        "rounded-lg border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive"
      }
    >
      <p className="font-medium">{title}</p>
      <p className="mt-1 text-destructive/90">{message}</p>
    </div>
  );
}

"use client";

import Link from "next/link";
import { format, parseISO } from "date-fns";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { useEmployeePortal, useStartShift } from "../hooks";
import { shiftStatusLabel } from "../utils/shiftDisplay";
import type { PortalShift } from "../types/employee-portal.types";

function formatUtc(iso: string): string {
  try {
    return format(parseISO(iso), "MMM d, yyyy h:mm a");
  } catch {
    return iso;
  }
}

function ShiftCard({
  shift,
  highlight,
}: {
  shift: PortalShift;
  highlight?: boolean;
}) {
  return (
    <div
      className={cn(
        "rounded-xl border bg-card p-4 shadow-sm",
        highlight ? "border-primary/50 ring-2 ring-primary/20" : "border-border",
      )}
    >
      <p className="font-semibold text-foreground">{shift.title}</p>
      {shift.description ? (
        <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{shift.description}</p>
      ) : null}
      <p className="mt-2 text-xs text-muted-foreground">
        {formatUtc(shift.startAtUtc)} – {formatUtc(shift.endAtUtc)}
      </p>
      <p className="mt-1 text-xs font-medium text-primary">{shiftStatusLabel(shift.status)}</p>
    </div>
  );
}

export function EmployeePortalSection() {
  const portalQuery = useEmployeePortal();
  const startShift = useStartShift();

  const nearest = portalQuery.data?.nearestUpcomingShift ?? null;
  const canStartNearest = nearest !== null && nearest.status === 0;

  const handleStartNearest = () => {
    if (!nearest) return;
    startShift.mutate(nearest.id);
  };

  if (portalQuery.isLoading) {
    return (
      <div className="mx-auto max-w-4xl space-y-6 px-4 py-10 sm:px-6">
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-40 w-full rounded-xl" />
        <Skeleton className="h-32 w-full rounded-xl" />
      </div>
    );
  }

  if (portalQuery.isError) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-10 sm:px-6">
        <div
          className="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive"
          role="alert"
        >
          {getErrorMessage(portalQuery.error)}
        </div>
      </div>
    );
  }

  const data = portalQuery.data;
  if (!data) return null;

  const topIds = new Set(data.topThreeUpcomingShifts.map((s) => s.id));

  return (
    <div className="mx-auto flex max-w-4xl flex-1 flex-col gap-10 px-4 py-10 sm:px-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Employee portal</h1>
        <p className="max-w-2xl text-muted-foreground">
          Your upcoming shifts and leave snapshot. Admins can schedule shifts for you in the main app.
        </p>
      </div>

      <section className="space-y-4" aria-labelledby="nearest-shift-heading">
        <h2 id="nearest-shift-heading" className="text-lg font-semibold text-foreground">
          Next shift
        </h2>
        {nearest ? (
          <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
            <ShiftCard shift={nearest} />
            <div className="mt-4 flex flex-wrap items-center gap-3">
              <Button
                type="button"
                onClick={handleStartNearest}
                disabled={!canStartNearest || startShift.isPending}
              >
                {startShift.isPending ? "Starting…" : "Start shift"}
              </Button>
              {!canStartNearest ? (
                <span className="text-sm text-muted-foreground">
                  {nearest.status !== 0
                    ? "This shift is not in a startable state."
                    : "You cannot start this shift right now."}
                </span>
              ) : null}
            </div>
            {startShift.isError ? (
              <p className="mt-2 text-sm text-destructive" role="alert">
                {getErrorMessage(startShift.error)}
              </p>
            ) : null}
          </div>
        ) : (
          <p className="rounded-xl border border-dashed border-border bg-muted/30 px-4 py-8 text-center text-sm text-muted-foreground">
            No upcoming shifts scheduled.
          </p>
        )}
      </section>

      <section className="space-y-4" aria-labelledby="upcoming-heading">
        <h2 id="upcoming-heading" className="text-lg font-semibold text-foreground">
          Upcoming shifts
        </h2>
        {data.topThreeUpcomingShifts.length > 0 ? (
          <div>
            <p className="mb-3 text-sm font-medium text-muted-foreground">Next three</p>
            <div className="grid gap-3 sm:grid-cols-3">
              {data.topThreeUpcomingShifts.map((shift) => (
                <ShiftCard key={shift.id} shift={shift} highlight />
              ))}
            </div>
          </div>
        ) : null}

        <div>
          <p className="mb-3 text-sm font-medium text-muted-foreground">All upcoming</p>
          {data.allUpcomingShifts.length > 0 ? (
            <ul className="space-y-2">
              {data.allUpcomingShifts.map((shift) => (
                <li key={shift.id}>
                  <ShiftCard shift={shift} highlight={topIds.has(shift.id)} />
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-muted-foreground">No further upcoming shifts.</p>
          )}
        </div>
      </section>

      <section className="space-y-4" aria-labelledby="leave-heading">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <h2 id="leave-heading" className="text-lg font-semibold text-foreground">
            Leave
          </h2>
          <Link
            href="/leave"
            className="inline-flex items-center justify-center rounded-lg border border-zinc-300 bg-white px-4 py-2 text-sm font-medium text-zinc-900 transition-colors hover:bg-zinc-50 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100 dark:hover:bg-zinc-800"
          >
            Open leave
          </Link>
        </div>

        {data.leaveBalances.length > 0 ? (
          <div className="grid gap-3 sm:grid-cols-2">
            {data.leaveBalances.map((b) => (
              <div key={b.id} className="rounded-xl border border-border bg-card p-4 shadow-sm">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Leave type #{b.leaveTypeId}
                </p>
                <p className="mt-2 text-2xl font-semibold tabular-nums text-foreground">
                  {b.availableAmount}
                </p>
                <p className="text-xs text-muted-foreground">Available (as of balance)</p>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No leave balances on file.</p>
        )}

        {data.leaveRequests.length > 0 ? (
          <div className="overflow-x-auto rounded-xl border border-border">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="border-b border-border bg-muted/50">
                <tr>
                  <th className="px-4 py-2 font-medium">From</th>
                  <th className="px-4 py-2 font-medium">To</th>
                  <th className="px-4 py-2 font-medium">Amount</th>
                  <th className="px-4 py-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.leaveRequests.map((r) => (
                  <tr key={r.id} className="border-b border-border last:border-0">
                    <td className="px-4 py-2 text-muted-foreground">{formatUtc(r.startDateUtc)}</td>
                    <td className="px-4 py-2 text-muted-foreground">{formatUtc(r.endDateUtc)}</td>
                    <td className="px-4 py-2 tabular-nums">{r.requestedAmount}</td>
                    <td className="px-4 py-2">{r.status}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No recent leave requests.</p>
        )}
      </section>
    </div>
  );
}

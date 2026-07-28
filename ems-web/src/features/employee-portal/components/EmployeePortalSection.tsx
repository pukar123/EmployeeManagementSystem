"use client";

import Link from "next/link";
import { format, parseISO } from "date-fns";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { useEmployeePortal, useStartShift, useStartTask } from "../hooks";
import { shiftStatusLabel } from "../utils/shiftDisplay";
import { taskPriorityLabels, taskStatusLabels } from "@/features/tasks/utils/taskDisplay";
import type { PortalScheduleEntry, PortalShift, PortalTask } from "../types/employee-portal.types";

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

function TaskCard({ task, highlight }: { task: PortalTask; highlight?: boolean }) {
  return (
    <div
      className={cn(
        "rounded-xl border bg-card p-4 shadow-sm",
        highlight ? "border-primary/50 ring-2 ring-primary/20" : "border-border",
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <p className="font-semibold text-foreground">{task.title}</p>
        <span className="shrink-0 rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
          {taskStatusLabels[task.status]}
        </span>
      </div>
      {task.description ? (
        <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{task.description}</p>
      ) : null}
      <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
        {task.priority ? <span>Priority: {taskPriorityLabels[task.priority]}</span> : null}
        <span>Due: {task.dueAtUtc ? formatUtc(task.dueAtUtc) : "—"}</span>
      </div>
    </div>
  );
}

function WorkEntryCard({ entry, highlight }: { entry: PortalScheduleEntry; highlight?: boolean }) {
  if (entry.kind === 0 && entry.shift) {
    return <ShiftCard shift={entry.shift} highlight={highlight} />;
  }
  if (entry.kind === 1 && entry.task) {
    return <TaskCard task={entry.task} highlight={highlight} />;
  }
  return null;
}

function heroHeading(entry: PortalScheduleEntry | null): string {
  if (!entry) return "Next work";
  if (entry.kind === 0 && entry.shift?.status === 1) return "Current shift";
  if (entry.kind === 1 && entry.task?.status === 2) return "Current task";
  return "Next work";
}

function entryCanBeStarted(entry: PortalScheduleEntry | null): boolean {
  if (!entry) return false;
  if (entry.kind === 0 && entry.shift) return entry.shift.status === 0;
  if (entry.kind === 1 && entry.task) {
    return entry.task.status === 1 || entry.task.status === 3;
  }
  return false;
}

export function EmployeePortalSection() {
  const portalQuery = useEmployeePortal();
  const startShift = useStartShift();
  const startTask = useStartTask();

  const schedule = portalQuery.data?.schedule ?? [];
  const nextEntry = schedule[0] ?? null;
  const rest = schedule.slice(1);

  const handleStartNext = () => {
    if (!nextEntry) return;
    if (nextEntry.kind === 0 && nextEntry.shift) {
      startShift.mutate(nextEntry.shift.id);
    } else if (nextEntry.kind === 1 && nextEntry.task) {
      startTask.mutate(nextEntry.task.id);
    }
  };

  const startPending = startShift.isPending || startTask.isPending;
  const startError = startShift.isError ? startShift.error : startTask.isError ? startTask.error : null;

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

  if (!data.hasLinkedEmployeeProfile) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-10 sm:px-6">
        <div className="space-y-4">
          <h1 className="text-3xl font-semibold tracking-tight text-foreground">Employee portal</h1>
          <div
            className="rounded-xl border border-border bg-muted/30 px-4 py-5 text-sm text-muted-foreground"
            role="status"
          >
            <p className="font-medium text-foreground">Employee portal is available</p>
            <p className="mt-2 leading-relaxed">
              You can access the employee portal. Shifts, tasks, and leave appear when the{" "}
              <span className="font-medium text-foreground">account you are signed in with</span> is linked to an
              employee (same user id stored on the employee record). If you used &quot;Link login&quot; for someone else
              or created a new login, sign in with that user to see their schedule here.
            </p>
          </div>
        </div>
      </div>
    );
  }

  const nextStartable = entryCanBeStarted(nextEntry);
  const heroShowsProgress =
    nextEntry &&
    ((nextEntry.kind === 0 && nextEntry.shift?.status === 1) ||
      (nextEntry.kind === 1 && nextEntry.task?.status === 2));

  return (
    <div className="mx-auto flex max-w-4xl flex-1 flex-col gap-10 px-4 py-10 sm:px-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Employee portal</h1>
        <p className="max-w-2xl text-muted-foreground">
          Your shifts, tasks, and leave in one place. Admins schedule work for you in the main app.
        </p>
      </div>

      <section className="space-y-4" aria-labelledby="next-work-heading">
        <h2 id="next-work-heading" className="text-lg font-semibold text-foreground">
          {heroHeading(nextEntry)}
        </h2>
        {nextEntry ? (
          <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
            <WorkEntryCard entry={nextEntry} />
            {nextStartable ? (
              <div className="mt-4 flex flex-wrap items-center gap-3">
                <Button type="button" onClick={handleStartNext} disabled={startPending}>
                  {startPending ? "Starting…" : "Start"}
                </Button>
                {startError ? (
                  <p className="w-full text-sm text-destructive" role="alert">
                    {getErrorMessage(startError)}
                  </p>
                ) : null}
              </div>
            ) : heroShowsProgress ? (
              <p className="mt-4 text-sm text-muted-foreground">This item is in progress.</p>
            ) : (
              <p className="mt-4 text-sm text-muted-foreground">This item cannot be started from here.</p>
            )}
          </div>
        ) : (
          <p className="rounded-xl border border-dashed border-border bg-muted/30 px-4 py-8 text-center text-sm text-muted-foreground">
            No shifts or open tasks for this period.
          </p>
        )}
      </section>

      {rest.length > 0 ? (
        <section className="space-y-4" aria-labelledby="schedule-heading">
          <h2 id="schedule-heading" className="text-lg font-semibold text-foreground">
            Your schedule
          </h2>
          <ul className="space-y-2">
            {rest.map((entry, idx) => (
              <li key={entry.kind === 0 && entry.shift ? `s-${entry.shift.id}` : `t-${entry.task!.id}`}>
                <WorkEntryCard entry={entry} highlight={idx < 2} />
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <section className="space-y-4" aria-labelledby="leave-heading">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <h2 id="leave-heading" className="text-lg font-semibold text-foreground">
            Leave
          </h2>
          <Link
            href="/employee-portal/leave"
            className="inline-flex items-center justify-center rounded-lg border border-input bg-background px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-muted/50 dark:bg-card dark:text-foreground dark:hover:bg-muted/60"
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
                <p className="mt-2 text-2xl font-semibold tabular-nums text-foreground">{b.availableAmount}</p>
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

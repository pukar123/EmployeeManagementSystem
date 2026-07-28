"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { useEmployees } from "@/features/employees/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { useAttendanceMutations, useAttendanceSummary, useEmployeeAttendance } from "../hooks";
import { AttendanceHistoryTable } from "./AttendanceHistoryTable";
import { BreakTrackerCard } from "./BreakTrackerCard";
import { CheckInOutCard } from "./CheckInOutCard";
import { ManualEntryModal } from "./ManualEntryModal";

async function getGpsCoords(): Promise<{ latitude: number; longitude: number } | null> {
  if (!("geolocation" in navigator)) return null;
  return new Promise((resolve) => {
    navigator.geolocation.getCurrentPosition(
      (pos) => resolve({ latitude: pos.coords.latitude, longitude: pos.coords.longitude }),
      () => resolve(null),
      { enableHighAccuracy: true, timeout: 5000, maximumAge: 60000 },
    );
  });
}

function toIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function AttendanceSection() {
  const { organizationId } = useOrganizationContext();
  const { data: employees = [], isLoading: employeesLoading } = useEmployees();
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [manualOpen, setManualOpen] = useState(false);
  const [fromDate, setFromDate] = useState(toIsoDate(new Date(new Date().setDate(new Date().getDate() - 7))));
  const [toDate, setToDate] = useState(toIsoDate(new Date()));

  const employeeId = selectedEmployeeId ?? employees[0]?.id ?? null;

  const attendanceQuery = useEmployeeAttendance(employeeId, fromDate, toDate);
  const summaryQuery = useAttendanceSummary(employeeId, fromDate, toDate);
  const { checkIn, checkOut, startBreak, endBreak, manualEntry } = useAttendanceMutations({
    employeeId: employeeId ?? 0,
    fromDate,
    toDate,
  });

  const rows = attendanceQuery.data ?? [];
  const latest = rows[0];
  const hasOpenSession = latest?.checkOutAtUtc == null;
  const hasOpenBreak = useMemo(() => {
    if (!latest) return false;
    return latest.breaks.some((b) => b.endAtUtc == null);
  }, [latest]);

  const pending = checkIn.isPending || checkOut.isPending || startBreak.isPending || endBreak.isPending;

  const doCheckIn = async () => {
    if (!employeeId) return;
    try {
      const gps = await getGpsCoords();
      await checkIn.mutateAsync({ employeeId, latitude: gps?.latitude, longitude: gps?.longitude });
      toast.success("Checked in.");
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  const doCheckOut = async () => {
    if (!employeeId) return;
    try {
      const gps = await getGpsCoords();
      await checkOut.mutateAsync({ employeeId, latitude: gps?.latitude, longitude: gps?.longitude });
      toast.success("Checked out.");
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  const doStartBreak = async () => {
    if (!employeeId) return;
    try {
      await startBreak.mutateAsync({ employeeId });
      toast.success("Break started.");
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  const doEndBreak = async () => {
    if (!employeeId) return;
    try {
      await endBreak.mutateAsync({ employeeId });
      toast.success("Break ended.");
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  if (employeesLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-foreground">Attendance</h1>
          <p className="mt-1 text-sm text-muted-foreground">Track check-ins, breaks, and working time.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Link
            href="/attendance/reports"
            className="inline-flex items-center rounded-xl border border-input px-3 py-2 text-sm font-medium text-foreground transition hover:bg-muted/60"
          >
            Reports
          </Link>
          <Link
            href="/attendance/analytics"
            className="inline-flex items-center rounded-xl border border-input px-3 py-2 text-sm font-medium text-foreground transition hover:bg-muted/60"
          >
            Analytics
          </Link>
          <Button type="button" variant="secondary" onClick={() => setManualOpen(true)} disabled={!employeeId}>
            Add manual entry
          </Button>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <label className="block">
          <span className="text-xs font-medium text-muted-foreground">Employee</span>
          <select
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
            value={employeeId ?? ""}
            onChange={(e) => setSelectedEmployeeId(e.target.value ? Number(e.target.value) : null)}
          >
            {employees.map((e) => (
              <option key={e.id} value={e.id}>
                {e.firstName} {e.lastName}
              </option>
            ))}
          </select>
        </label>
        <label className="block">
          <span className="text-xs font-medium text-muted-foreground">From</span>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
          />
        </label>
        <label className="block">
          <span className="text-xs font-medium text-muted-foreground">To</span>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
          />
        </label>
      </div>

      {summaryQuery.data ? (
        <div className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
            <p className="text-xs text-muted-foreground">Records</p>
            <p className="mt-1 text-2xl font-semibold">{summaryQuery.data.totalRecords}</p>
          </div>
          <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
            <p className="text-xs text-muted-foreground">Worked minutes</p>
            <p className="mt-1 text-2xl font-semibold">{summaryQuery.data.totalWorkedMinutes}</p>
          </div>
          <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
            <p className="text-xs text-muted-foreground">Break minutes</p>
            <p className="mt-1 text-2xl font-semibold">{summaryQuery.data.totalBreakMinutes}</p>
          </div>
        </div>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2">
        <CheckInOutCard hasOpenSession={hasOpenSession} pending={pending} onCheckIn={() => void doCheckIn()} onCheckOut={() => void doCheckOut()} />
        <BreakTrackerCard
          hasOpenSession={hasOpenSession}
          hasOpenBreak={hasOpenBreak}
          pending={pending}
          onStartBreak={() => void doStartBreak()}
          onEndBreak={() => void doEndBreak()}
        />
      </div>

      {attendanceQuery.isLoading ? (
        <div className="flex justify-center py-12">
          <Spinner />
        </div>
      ) : attendanceQuery.isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
          {getErrorMessage(attendanceQuery.error)}
        </div>
      ) : (
        <AttendanceHistoryTable rows={rows} />
      )}

      {employeeId != null && organizationId != null ? (
        <ManualEntryModal
          open={manualOpen}
          organizationId={organizationId}
          employeeId={employeeId}
          pending={manualEntry.isPending}
          onClose={() => setManualOpen(false)}
          onSubmit={async (payload) => {
            try {
              await manualEntry.mutateAsync(payload);
              toast.success("Manual attendance saved.");
              setManualOpen(false);
            } catch (e) {
              toast.error(getErrorMessage(e));
            }
          }}
        />
      ) : null}
    </div>
  );
}

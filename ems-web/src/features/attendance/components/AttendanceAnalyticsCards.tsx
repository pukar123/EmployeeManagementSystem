"use client";

import type { AttendanceAbsenteeismAnalytics, AttendancePunctualityAnalytics } from "../types/attendance-analytics.types";

type AttendanceAnalyticsCardsProps = {
  punctuality: AttendancePunctualityAnalytics | undefined;
  absenteeism: AttendanceAbsenteeismAnalytics | undefined;
};

export function AttendanceAnalyticsCards({ punctuality, absenteeism }: AttendanceAnalyticsCardsProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <p className="text-xs text-muted-foreground">Late arrivals</p>
        <p className="mt-1 text-2xl font-semibold">{punctuality?.lateArrivals ?? 0}</p>
      </div>
      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <p className="text-xs text-muted-foreground">Early departures</p>
        <p className="mt-1 text-2xl font-semibold">{punctuality?.earlyDepartures ?? 0}</p>
      </div>
      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <p className="text-xs text-muted-foreground">Absent days</p>
        <p className="mt-1 text-2xl font-semibold">{absenteeism?.absentDays ?? 0}</p>
      </div>
      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <p className="text-xs text-muted-foreground">Absence rate</p>
        <p className="mt-1 text-2xl font-semibold">{absenteeism?.absenceRate ?? 0}%</p>
      </div>
    </div>
  );
}

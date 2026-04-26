"use client";

import type { AttendanceAbsenteeismAnalytics, AttendancePunctualityAnalytics } from "../types/attendance-analytics.types";

type AttendanceAnalyticsCardsProps = {
  punctuality: AttendancePunctualityAnalytics | undefined;
  absenteeism: AttendanceAbsenteeismAnalytics | undefined;
};

export function AttendanceAnalyticsCards({ punctuality, absenteeism }: AttendanceAnalyticsCardsProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <p className="text-xs text-zinc-500">Late arrivals</p>
        <p className="mt-1 text-2xl font-semibold">{punctuality?.lateArrivals ?? 0}</p>
      </div>
      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <p className="text-xs text-zinc-500">Early departures</p>
        <p className="mt-1 text-2xl font-semibold">{punctuality?.earlyDepartures ?? 0}</p>
      </div>
      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <p className="text-xs text-zinc-500">Absent days</p>
        <p className="mt-1 text-2xl font-semibold">{absenteeism?.absentDays ?? 0}</p>
      </div>
      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <p className="text-xs text-zinc-500">Absence rate</p>
        <p className="mt-1 text-2xl font-semibold">{absenteeism?.absenceRate ?? 0}%</p>
      </div>
    </div>
  );
}

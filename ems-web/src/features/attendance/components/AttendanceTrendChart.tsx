"use client";

import type { AttendancePeriodSummary } from "../types/attendance-report.types";

type AttendanceTrendChartProps = {
  rows: AttendancePeriodSummary[];
};

export function AttendanceTrendChart({ rows }: AttendanceTrendChartProps) {
  const maxWorked = Math.max(...rows.map((x) => x.totalWorkedMinutes), 1);

  return (
    <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
      <h3 className="text-sm font-semibold">Worked minutes trend</h3>
      <div className="mt-4 space-y-3">
        {rows.map((row) => (
          <div key={row.periodLabel}>
            <div className="mb-1 flex items-center justify-between text-xs text-zinc-500">
              <span>{row.periodLabel}</span>
              <span>{row.totalWorkedMinutes}</span>
            </div>
            <div className="h-2 rounded-full bg-zinc-200 dark:bg-zinc-800">
              <div
                className="h-2 rounded-full bg-blue-600"
                style={{ width: `${Math.max(4, (row.totalWorkedMinutes / maxWorked) * 100)}%` }}
              />
            </div>
          </div>
        ))}
        {rows.length === 0 ? <p className="text-sm text-zinc-500">No trend data yet.</p> : null}
      </div>
    </div>
  );
}

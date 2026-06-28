"use client";

import type { AttendancePeriodSummary } from "../types/attendance-report.types";

type AttendanceTrendChartProps = {
  rows: AttendancePeriodSummary[];
};

export function AttendanceTrendChart({ rows }: AttendanceTrendChartProps) {
  const maxWorked = Math.max(...rows.map((x) => x.totalWorkedMinutes), 1);

  return (
    <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
      <h3 className="text-sm font-semibold">Worked minutes trend</h3>
      <div className="mt-4 space-y-3">
        {rows.map((row) => (
          <div key={row.periodLabel}>
            <div className="mb-1 flex items-center justify-between text-xs text-muted-foreground">
              <span>{row.periodLabel}</span>
              <span>{row.totalWorkedMinutes}</span>
            </div>
            <div className="h-2 rounded-full bg-muted">
              <div
                className="h-2 rounded-full bg-blue-600"
                style={{ width: `${Math.max(4, (row.totalWorkedMinutes / maxWorked) * 100)}%` }}
              />
            </div>
          </div>
        ))}
        {rows.length === 0 ? <p className="text-sm text-muted-foreground">No trend data yet.</p> : null}
      </div>
    </div>
  );
}

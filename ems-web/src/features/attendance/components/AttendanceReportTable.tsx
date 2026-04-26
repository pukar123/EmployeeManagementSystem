"use client";

import type { AttendanceDailySummary, AttendancePeriodSummary } from "../types/attendance-report.types";

type AttendanceReportTableProps = {
  dailyRows: AttendanceDailySummary[];
  periodRows: AttendancePeriodSummary[];
};

export function AttendanceReportTable({ dailyRows, periodRows }: AttendanceReportTableProps) {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white dark:border-zinc-700 dark:bg-zinc-950">
        <div className="border-b border-zinc-200 px-4 py-3 dark:border-zinc-700">
          <h3 className="text-sm font-semibold">Daily summary</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead className="bg-zinc-50 text-left text-xs uppercase text-zinc-600 dark:bg-zinc-900 dark:text-zinc-400">
              <tr>
                <th className="px-3 py-2">Date</th>
                <th className="px-3 py-2">Present</th>
                <th className="px-3 py-2">Worked</th>
                <th className="px-3 py-2">Break</th>
              </tr>
            </thead>
            <tbody>
              {dailyRows.map((row) => (
                <tr key={row.workDate} className="border-t border-zinc-100 dark:border-zinc-800">
                  <td className="px-3 py-2">{row.workDate.slice(0, 10)}</td>
                  <td className="px-3 py-2">{row.presentEmployees}</td>
                  <td className="px-3 py-2">{row.totalWorkedMinutes}</td>
                  <td className="px-3 py-2">{row.totalBreakMinutes}</td>
                </tr>
              ))}
              {dailyRows.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-3 py-4 text-center text-zinc-500">
                    No daily rows found for this range.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white dark:border-zinc-700 dark:bg-zinc-950">
        <div className="border-b border-zinc-200 px-4 py-3 dark:border-zinc-700">
          <h3 className="text-sm font-semibold">Weekly/monthly summary</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead className="bg-zinc-50 text-left text-xs uppercase text-zinc-600 dark:bg-zinc-900 dark:text-zinc-400">
              <tr>
                <th className="px-3 py-2">Period</th>
                <th className="px-3 py-2">Records</th>
                <th className="px-3 py-2">Worked</th>
                <th className="px-3 py-2">Avg</th>
              </tr>
            </thead>
            <tbody>
              {periodRows.map((row) => (
                <tr key={row.periodLabel} className="border-t border-zinc-100 dark:border-zinc-800">
                  <td className="px-3 py-2">{row.periodLabel}</td>
                  <td className="px-3 py-2">{row.totalRecords}</td>
                  <td className="px-3 py-2">{row.totalWorkedMinutes}</td>
                  <td className="px-3 py-2">{row.averageWorkedMinutes}</td>
                </tr>
              ))}
              {periodRows.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-3 py-4 text-center text-zinc-500">
                    No period rows found for this range.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

"use client";

import type { AttendanceRecord } from "../types/attendance.types";

type AttendanceHistoryTableProps = {
  rows: AttendanceRecord[];
};

function fmt(dt: string | null): string {
  if (!dt) return "—";
  return new Date(dt).toLocaleString();
}

export function AttendanceHistoryTable({ rows }: AttendanceHistoryTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-700">
      <table className="min-w-full divide-y divide-zinc-200 text-left text-sm dark:divide-zinc-700">
        <thead className="bg-zinc-50 dark:bg-zinc-900/50">
          <tr>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">#</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Work date</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Check-in</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Check-out</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Break (min)</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Worked (min)</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-zinc-200 dark:divide-zinc-700">
          {rows.map((row, index) => (
            <tr key={row.id} className="bg-white hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900">
              <td className="px-4 py-3 font-mono text-zinc-600 dark:text-zinc-400">{index + 1}</td>
              <td className="px-4 py-3">{new Date(row.workDate).toLocaleDateString()}</td>
              <td className="px-4 py-3">{fmt(row.checkInAtUtc)}</td>
              <td className="px-4 py-3">{fmt(row.checkOutAtUtc)}</td>
              <td className="px-4 py-3">{row.breakMinutes}</td>
              <td className="px-4 py-3">{row.workedMinutes}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <p className="p-6 text-center text-sm text-zinc-500">No attendance records found.</p> : null}
    </div>
  );
}

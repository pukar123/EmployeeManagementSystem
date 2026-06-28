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
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="min-w-full divide-y divide-border text-left text-sm ">
        <thead className="bg-muted/50">
          <tr>
            <th className="px-4 py-3 font-medium text-muted-foreground">#</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Work date</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Check-in</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Check-out</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Break (min)</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Worked (min)</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row, index) => (
            <tr key={row.id} className="bg-card hover:bg-muted/40">
              <td className="px-4 py-3 font-mono text-muted-foreground">{index + 1}</td>
              <td className="px-4 py-3">{new Date(row.workDate).toLocaleDateString()}</td>
              <td className="px-4 py-3">{fmt(row.checkInAtUtc)}</td>
              <td className="px-4 py-3">{fmt(row.checkOutAtUtc)}</td>
              <td className="px-4 py-3">{row.breakMinutes}</td>
              <td className="px-4 py-3">{row.workedMinutes}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {rows.length === 0 ? <p className="p-6 text-center text-sm text-muted-foreground">No attendance records found.</p> : null}
    </div>
  );
}

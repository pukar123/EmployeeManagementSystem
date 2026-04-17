"use client";

import type { Employee } from "../types/employee.types";
import { employmentStatusLabels } from "../types/employment-status";
import { Button } from "@/shared/components/Button";

type EmployeeTableProps = {
  employees: Employee[];
  /** Resolved display labels for job positions; avoids showing raw ids in the grid. */
  jobPositionLabelById: Map<number, string>;
  /** Per employee, shows immediate manager's position code; top-level falls back to own position code. */
  immediateManagerPositionByEmployeeId: Map<number, string>;
  onViewHistory: (e: Employee) => void;
  onEdit: (e: Employee) => void;
  onDelete: (e: Employee) => void;
};

function formatJobPositionCell(
  jobPositionId: number | null,
  jobPositionLabelById: Map<number, string>,
): string {
  if (jobPositionId == null) return "—";
  return jobPositionLabelById.get(jobPositionId) ?? "—";
}

export function EmployeeTable({
  employees,
  jobPositionLabelById,
  immediateManagerPositionByEmployeeId,
  onViewHistory,
  onEdit,
  onDelete,
}: EmployeeTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-700">
      <table className="min-w-full divide-y divide-zinc-200 text-left text-sm dark:divide-zinc-700">
        <thead className="bg-zinc-50 dark:bg-zinc-900/50">
          <tr>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">#</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Employee #</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Name</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Email</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Status</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Immediate Manager</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Position</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-zinc-200 dark:divide-zinc-700">
          {employees.map((row, index) => (
            <tr key={row.id} className="bg-white hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900">
              <td className="whitespace-nowrap px-4 py-3 font-mono text-zinc-600 dark:text-zinc-400">
                {index + 1}
              </td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-zinc-600 dark:text-zinc-400">
                {row.employeeNumber}
              </td>
              <td className="px-4 py-3 text-zinc-900 dark:text-zinc-100">
                {row.firstName} {row.lastName}
              </td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{row.email}</td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">
                {employmentStatusLabels[row.employmentStatus] ?? row.employmentStatus}
              </td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">
                {immediateManagerPositionByEmployeeId.get(row.id) ?? "—"}
              </td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">
                {formatJobPositionCell(row.jobPositionId, jobPositionLabelById)}
              </td>
              <td className="whitespace-nowrap px-4 py-3">
                <div className="flex gap-2">
                  <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => onViewHistory(row)}>
                    History
                  </Button>
                  <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => onEdit(row)}>
                    Edit
                  </Button>
                  <Button type="button" variant="danger" className="!py-1 !text-xs" onClick={() => onDelete(row)}>
                    Delete
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {employees.length === 0 ? (
        <p className="p-6 text-center text-sm text-zinc-500">No employees match the current filter.</p>
      ) : null}
    </div>
  );
}

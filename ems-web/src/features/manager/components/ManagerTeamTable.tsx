"use client";

import Link from "next/link";
import { EmploymentStatusPill } from "@/features/employees/components/EmploymentStatusPill";
import {
  DataTable,
  DataTableBody,
  DataTableCell,
  DataTableElement,
  DataTableHead,
  DataTableHeaderCell,
  DataTableRow,
} from "@/shared/components/DataTable";
import { cn } from "@/shared/utils/cn";
import {
  todayAttendanceStatusLabels,
  type ManagerTeamMember,
  type ManagerTeamTodayAttendanceStatus,
} from "../types/manager-team.types";

type ManagerTeamTableProps = {
  members: ManagerTeamMember[];
  sortBy?: string;
  sortDirection?: "asc" | "desc";
  onSort?: (sortBy: string) => void;
};

type SortableColumn = {
  key: string;
  label: string;
  sortKey?: string;
};

const columns: SortableColumn[] = [
  { key: "number", label: "Employee #", sortKey: "employeenumber" },
  { key: "name", label: "Name", sortKey: "name" },
  { key: "status", label: "Status", sortKey: "employmentstatus" },
  { key: "department", label: "Department" },
  { key: "position", label: "Position" },
  { key: "today", label: "Today" },
  { key: "leave", label: "Pending leave" },
  { key: "tasks", label: "Overdue tasks" },
  { key: "changes", label: "Upcoming" },
  { key: "actions", label: "Actions" },
];

const attendancePillClass: Record<ManagerTeamTodayAttendanceStatus, string> = {
  0: "bg-emerald-500/10 text-emerald-700 dark:text-emerald-400",
  1: "bg-rose-500/10 text-rose-700 dark:text-rose-400",
  2: "bg-amber-500/10 text-amber-700 dark:text-amber-400",
  3: "bg-sky-500/10 text-sky-700 dark:text-sky-400",
};

export function ManagerTeamTable({
  members,
  sortBy = "name",
  sortDirection = "asc",
  onSort,
}: ManagerTeamTableProps) {
  return (
    <div className="overflow-x-auto">
      <DataTable isEmpty={members.length === 0} emptyMessage="No direct reports match the current filters.">
        <DataTableElement>
          <DataTableHead>
            <tr>
              {columns.map((col) => (
                <DataTableHeaderCell key={col.key}>
                  {col.sortKey && onSort ? (
                    <button
                      type="button"
                      className={cn(
                        "inline-flex items-center gap-1 font-medium hover:text-primary",
                        sortBy === col.sortKey && "text-primary",
                      )}
                      onClick={() => onSort(col.sortKey!)}
                    >
                      {col.label}
                      {sortBy === col.sortKey ? (
                        <span aria-hidden>{sortDirection === "asc" ? "↑" : "↓"}</span>
                      ) : null}
                    </button>
                  ) : (
                    col.label
                  )}
                </DataTableHeaderCell>
              ))}
            </tr>
          </DataTableHead>
          <DataTableBody>
            {members.map((row) => (
              <DataTableRow key={row.id}>
                <DataTableCell className="whitespace-nowrap font-mono text-muted-foreground">
                  {row.employeeNumber}
                </DataTableCell>
                <DataTableCell className="font-medium">
                  <Link href={`/employees/${row.id}`} className="text-primary hover:underline">
                    {row.firstName} {row.lastName}
                  </Link>
                </DataTableCell>
                <DataTableCell>
                  <EmploymentStatusPill status={row.employmentStatus} />
                </DataTableCell>
                <DataTableCell className="text-muted-foreground">{row.departmentName ?? "—"}</DataTableCell>
                <DataTableCell className="text-muted-foreground">{row.jobPositionTitle ?? "—"}</DataTableCell>
                <DataTableCell>
                  <span
                    className={cn(
                      "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                      attendancePillClass[row.todayAttendanceStatus],
                    )}
                  >
                    {todayAttendanceStatusLabels[row.todayAttendanceStatus]}
                  </span>
                </DataTableCell>
                <DataTableCell className="tabular-nums text-muted-foreground">{row.pendingLeaveCount}</DataTableCell>
                <DataTableCell className="tabular-nums text-muted-foreground">{row.overdueTaskCount}</DataTableCell>
                <DataTableCell className="tabular-nums text-muted-foreground">
                  {row.upcomingScheduledChangeCount}
                </DataTableCell>
                <DataTableCell>
                  <Link
                    href={`/employees/${row.id}`}
                    className="inline-flex items-center rounded-lg px-2 py-1 text-sm font-medium text-primary hover:underline"
                  >
                    View profile
                  </Link>
                </DataTableCell>
              </DataTableRow>
            ))}
          </DataTableBody>
        </DataTableElement>
      </DataTable>
    </div>
  );
}

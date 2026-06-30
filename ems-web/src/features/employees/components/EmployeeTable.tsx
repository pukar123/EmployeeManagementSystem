"use client";

import Link from "next/link";
import type { EmployeeDirectoryItem } from "../types/employee.types";
import { EmploymentStatusPill } from "./EmploymentStatusPill";
import { Button } from "@/shared/components/Button";
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

type EmployeeTableProps = {
  employees: EmployeeDirectoryItem[];
  isArchiveView: boolean;
  onRestore?: (employee: EmployeeDirectoryItem) => void;
  restoreBusy?: boolean;
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
  { key: "email", label: "Email" },
  { key: "status", label: "Status", sortKey: "employmentstatus" },
  { key: "manager", label: "Manager" },
  { key: "position", label: "Position" },
  { key: "site", label: "Site" },
  { key: "actions", label: "Actions" },
];

export function EmployeeTable({
  employees,
  isArchiveView,
  onRestore,
  restoreBusy,
  sortBy = "name",
  sortDirection = "asc",
  onSort,
}: EmployeeTableProps) {
  return (
    <div className="overflow-x-auto">
      <DataTable isEmpty={employees.length === 0} emptyMessage="No employees match the current filters.">
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
            {employees.map((row) => (
              <DataTableRow key={row.id}>
                <DataTableCell className="whitespace-nowrap font-mono text-muted-foreground">
                  {row.employeeNumber}
                </DataTableCell>
                <DataTableCell className="font-medium">
                  <Link href={`/employees/${row.id}`} className="text-primary hover:underline">
                    {row.firstName} {row.lastName}
                  </Link>
                </DataTableCell>
                <DataTableCell className="text-muted-foreground">{row.email}</DataTableCell>
                <DataTableCell>
                  <EmploymentStatusPill status={row.employmentStatus} />
                </DataTableCell>
                <DataTableCell className="text-muted-foreground">
                  {row.managerName
                    ? `${row.managerName}${row.managerEmployeeNumber ? ` (${row.managerEmployeeNumber})` : ""}`
                    : "No manager"}
                </DataTableCell>
                <DataTableCell className="text-muted-foreground">{row.jobPositionTitle ?? "—"}</DataTableCell>
                <DataTableCell className="text-muted-foreground">{row.primarySiteName ?? "—"}</DataTableCell>
                <DataTableCell>
                  <div className="flex flex-wrap gap-1">
                    <Link
                      href={`/employees/${row.id}`}
                      className="inline-flex items-center rounded-lg px-2 py-1 text-sm font-medium text-primary hover:underline"
                    >
                      View profile
                    </Link>
                    {isArchiveView && onRestore ? (
                      <Button
                        type="button"
                        variant="secondary"
                        size="sm"
                        disabled={restoreBusy}
                        onClick={() => onRestore(row)}
                      >
                        Restore
                      </Button>
                    ) : null}
                  </div>
                </DataTableCell>
              </DataTableRow>
            ))}
          </DataTableBody>
        </DataTableElement>
      </DataTable>
    </div>
  );
}

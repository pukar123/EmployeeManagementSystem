"use client";

import type { Employee } from "../types/employee.types";
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

type EmployeeTableProps = {
  employees: Employee[];
  jobPositionLabelById: Map<number, string>;
  immediateManagerPositionByEmployeeId: Map<number, string>;
  onViewHistory: (e: Employee) => void;
  onManageRoles: (e: Employee) => void;
  onProvisionUser: (e: Employee) => void;
  provisionBusy?: boolean;
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
  onManageRoles,
  onProvisionUser,
  provisionBusy = false,
  onEdit,
  onDelete,
}: EmployeeTableProps) {
  return (
    <DataTable isEmpty={employees.length === 0} emptyMessage="No employees match the current filter.">
      <DataTableElement>
        <DataTableHead>
          <tr>
            <DataTableHeaderCell>#</DataTableHeaderCell>
            <DataTableHeaderCell>Employee #</DataTableHeaderCell>
            <DataTableHeaderCell>Name</DataTableHeaderCell>
            <DataTableHeaderCell>Email</DataTableHeaderCell>
            <DataTableHeaderCell>Status</DataTableHeaderCell>
            <DataTableHeaderCell>Immediate Manager</DataTableHeaderCell>
            <DataTableHeaderCell>Position</DataTableHeaderCell>
            <DataTableHeaderCell>Actions</DataTableHeaderCell>
          </tr>
        </DataTableHead>
        <DataTableBody>
          {employees.map((row, index) => (
            <DataTableRow key={row.id}>
              <DataTableCell className="whitespace-nowrap font-mono text-muted-foreground">
                {index + 1}
              </DataTableCell>
              <DataTableCell className="whitespace-nowrap font-mono text-muted-foreground">
                {row.employeeNumber}
              </DataTableCell>
              <DataTableCell className="font-medium">
                {row.firstName} {row.lastName}
              </DataTableCell>
              <DataTableCell className="text-muted-foreground">{row.email}</DataTableCell>
              <DataTableCell>
                <EmploymentStatusPill status={row.employmentStatus} />
              </DataTableCell>
              <DataTableCell className="text-muted-foreground">
                {immediateManagerPositionByEmployeeId.get(row.id) ?? "—"}
              </DataTableCell>
              <DataTableCell className="text-muted-foreground">
                {formatJobPositionCell(row.jobPositionId, jobPositionLabelById)}
              </DataTableCell>
              <DataTableCell className="whitespace-nowrap">
                <div className="flex flex-wrap gap-1.5">
                  <Button type="button" variant="secondary" size="sm" onClick={() => onViewHistory(row)}>
                    History
                  </Button>
                  <Button type="button" variant="secondary" size="sm" onClick={() => onManageRoles(row)}>
                    Roles
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    disabled={row.isArchived || provisionBusy}
                    onClick={() => onProvisionUser(row)}
                  >
                    Link login
                  </Button>
                  <Button type="button" variant="secondary" size="sm" onClick={() => onEdit(row)}>
                    Edit
                  </Button>
                  <Button type="button" variant="danger" size="sm" onClick={() => onDelete(row)}>
                    Delete
                  </Button>
                </div>
              </DataTableCell>
            </DataTableRow>
          ))}
        </DataTableBody>
      </DataTableElement>
    </DataTable>
  );
}

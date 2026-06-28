"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useEmployees } from "../hooks";
import { useDepartments } from "@/features/departments/hooks";
import { useJobPositions } from "@/features/job-positions/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { SearchableSelect } from "@/shared/components/SearchableSelect";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { toSelectOptions } from "@/shared/utils/to-select-options";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";
import { DepartmentTransferForm } from "./DepartmentTransferForm";
import { PositionTransferForm } from "./PositionTransferForm";

function formatEmployeeLabel(e: { id: number; firstName: string; lastName: string; employeeNumber: string }): string {
  return `${e.firstName} ${e.lastName} (${e.employeeNumber})`;
}

export function EmployeeTransfersSection() {
  const queryClient = useQueryClient();
  const { organizationId } = useOrganizationContext();
  const { data: employees = [], isLoading: employeesLoading, isError: employeesError, error: employeesErr } = useEmployees();
  const { data: departments = [], isLoading: departmentsLoading } = useDepartments();
  const { data: positions = [], isLoading: positionsLoading } = useJobPositions(organizationId);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);

  const transferMutation = useMutation({
    mutationFn: async (args: { employeeId: number; data: Parameters<typeof employeeService.updateEmployee>[1] }) =>
      employeeService.updateEmployee(args.employeeId, args.data),
    onSuccess: async (updatedEmployee) => {
      await queryClient.invalidateQueries({ queryKey: employeeKeys.list() });
      await queryClient.invalidateQueries({ queryKey: employeeKeys.detail(updatedEmployee.id) });
      await queryClient.invalidateQueries({ queryKey: ["employees", "history", updatedEmployee.id] });
    },
  });

  const employeeOptions = useMemo(
    () =>
      toSelectOptions(
        employees.filter((e) => !e.isArchived),
        (e) => e.id,
        formatEmployeeLabel,
      ),
    [employees],
  );

  const selectedEmployee = useMemo(
    () => employees.find((e) => e.id === selectedEmployeeId) ?? null,
    [employees, selectedEmployeeId],
  );

  if (employeesLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (employeesError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400" role="alert">
        {getErrorMessage(employeesErr)}
      </p>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-foreground">Employee Transfers</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Transfer employees between departments and positions with effective dates and reasons.
          </p>
        </div>
        <Link href="/employees" className="text-sm text-muted-foreground underline underline-offset-4 dark:text-muted-foreground">
          Back to employees
        </Link>
      </div>

      <div className="max-w-xl space-y-2">
        <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Employee</label>
        <SearchableSelect<number>
          options={employeeOptions}
          value={selectedEmployeeId}
          onChange={setSelectedEmployeeId}
          placeholder="Search employees…"
          emptyLabel="Select employee"
        />
      </div>

      {!selectedEmployee ? (
        <p className="text-sm text-muted-foreground">Select an employee to start transfer actions.</p>
      ) : (
        <div className="grid gap-6 lg:grid-cols-2">
          <DepartmentTransferForm
            employee={selectedEmployee}
            departments={departments}
            loadingOptions={departmentsLoading}
            saving={transferMutation.isPending}
            onSubmit={async (values) => {
              const payload = {
                ...selectedEmployee,
                departmentId: values.newDepartmentId,
                departmentEffectiveFromUtc: new Date(values.effectiveFromUtc).toISOString(),
                departmentChangeReason: values.reason?.trim() || null,
              };
              await transferMutation.mutateAsync({ employeeId: selectedEmployee.id, data: payload });
              toast.success("Department transfer saved.");
            }}
          />

          <PositionTransferForm
            employee={selectedEmployee}
            positions={positions}
            loadingOptions={positionsLoading}
            saving={transferMutation.isPending}
            onSubmit={async (values) => {
              const payload = {
                ...selectedEmployee,
                jobPositionId: values.newJobPositionId,
                positionEffectiveFromUtc: new Date(values.effectiveFromUtc).toISOString(),
                positionChangeReason: values.reason?.trim() || null,
              };
              await transferMutation.mutateAsync({ employeeId: selectedEmployee.id, data: payload });
              toast.success("Position transfer saved.");
            }}
          />
        </div>
      )}
    </div>
  );
}

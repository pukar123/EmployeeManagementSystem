"use client";

import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useJobPositions } from "@/features/job-positions/hooks";
import type { JobPosition } from "@/features/job-positions/types/job-position.types";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { EmployeeTable } from "./EmployeeTable";
import { EmployeeForm } from "./EmployeeForm";
import { DeleteEmployeeDialog } from "./DeleteEmployeeDialog";
import { getErrorMessage } from "@/shared/api/http-client";
import { useEmployees } from "../hooks";
import { employeeKeys } from "../services/query-keys";
import { useEmployeeUiStore } from "../store/employee-ui-store";

function jobPositionLabel(j: JobPosition): string {
  return j.code ? `${j.title} (${j.code})` : j.title;
}

export function EmployeesSection() {
  const queryClient = useQueryClient();
  const { organizationId } = useOrganizationContext();
  const { data, isLoading, isError, error } = useEmployees();
  const { data: jobPositions = [] } = useJobPositions(organizationId);

  const jobPositionLabelById = useMemo(() => {
    const map = new Map<number, string>();
    for (const j of jobPositions) {
      map.set(j.id, jobPositionLabel(j));
    }
    return map;
  }, [jobPositions]);
  const [search, setSearch] = useState("");

  const formMode = useEmployeeUiStore((s) => s.formMode);
  const selectedEmployee = useEmployeeUiStore((s) => s.selectedEmployee);
  const openCreateForm = useEmployeeUiStore((s) => s.openCreateForm);
  const openEditForm = useEmployeeUiStore((s) => s.openEditForm);
  const closeForm = useEmployeeUiStore((s) => s.closeForm);

  const isDeleteOpen = useEmployeeUiStore((s) => s.isDeleteOpen);
  const employeeToDelete = useEmployeeUiStore((s) => s.employeeToDelete);
  const openDeleteDialog = useEmployeeUiStore((s) => s.openDeleteDialog);
  const closeDeleteDialog = useEmployeeUiStore((s) => s.closeDeleteDialog);

  const filtered = useMemo(() => {
    const list = data ?? [];
    const q = search.trim().toLowerCase();
    if (!q) return list;
    return list.filter((e) => {
      const hay = `${e.firstName} ${e.lastName} ${e.email} ${e.employeeNumber}`.toLowerCase();
      return hay.includes(q);
    });
  }, [data, search]);

  const refetchList = () => {
    void queryClient.invalidateQueries({ queryKey: employeeKeys.list() });
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Employees</h1>
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            View and manage your employee records.
          </p>
        </div>
        <Button type="button" onClick={openCreateForm}>
          Add employee
        </Button>
      </div>

      <div className="max-w-md">
        <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Search
        </label>
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Name, email, or employee #"
          className="mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100"
        />
      </div>

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      ) : isError ? (
        <div
          className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200"
          role="alert"
        >
          {getErrorMessage(error)}
        </div>
      ) : (
        <EmployeeTable
          employees={filtered}
          jobPositionLabelById={jobPositionLabelById}
          onEdit={(e) => openEditForm(e)}
          onDelete={(e) => openDeleteDialog(e)}
        />
      )}

      <Modal
        open={formMode != null}
        title={formMode === "create" ? "New employee" : "Edit employee"}
        onClose={closeForm}
        className="max-w-2xl"
      >
        {formMode ? (
          <EmployeeForm
            mode={formMode === "create" ? "create" : "edit"}
            employee={formMode === "edit" ? selectedEmployee : null}
            onSuccess={refetchList}
            onCancel={closeForm}
          />
        ) : null}
      </Modal>

      <DeleteEmployeeDialog
        employee={employeeToDelete}
        open={isDeleteOpen}
        onClose={closeDeleteDialog}
        onDeleted={refetchList}
      />
    </div>
  );
}

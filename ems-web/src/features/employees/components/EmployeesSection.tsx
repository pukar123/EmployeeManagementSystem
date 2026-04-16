"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useJobPositions } from "@/features/job-positions/hooks";
import type { JobPosition } from "@/features/job-positions/types/job-position.types";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { cn } from "@/shared/utils/cn";
import { EmployeeTable } from "./EmployeeTable";
import { EmployeeForm } from "./EmployeeForm";
import { DeleteEmployeeDialog } from "./DeleteEmployeeDialog";
import { getErrorMessage } from "@/shared/api/http-client";
import { fetchRoles } from "@/features/user-management/services/userManagementApi";
import type { RoleDto } from "@/features/user-management/types";
import { useAssignEmployeeUserRoles, useEmployees, useProvisionEmployeeUser } from "../hooks";
import { employeeKeys } from "../services/query-keys";
import { useEmployeeUiStore } from "../store/employee-ui-store";
import type { Employee, ProvisionEmployeeUserResponse } from "../types/employee.types";

function jobPositionLabel(j: JobPosition): string {
  return j.code ? `${j.title} (${j.code})` : j.title;
}

export function EmployeesSection() {
  const queryClient = useQueryClient();
  const { organizationId } = useOrganizationContext();
  const { data, isLoading, isError, error } = useEmployees();
  const { data: jobPositions = [] } = useJobPositions(organizationId);
  const rolesQuery = useQuery({ queryKey: ["roles"], queryFn: fetchRoles });
  const provisionMutation = useProvisionEmployeeUser();
  const assignRolesMutation = useAssignEmployeeUserRoles();

  const jobPositionLabelById = useMemo(() => {
    const map = new Map<number, string>();
    for (const j of jobPositions) {
      map.set(j.id, jobPositionLabel(j));
    }
    return map;
  }, [jobPositions]);
  const jobPositionCodeById = useMemo(() => {
    const map = new Map<number, string>();
    for (const j of jobPositions) {
      map.set(j.id, j.code?.trim() || j.title);
    }
    return map;
  }, [jobPositions]);
  const immediateManagerPositionByEmployeeId = useMemo(() => {
    const all = data ?? [];
    const employeeById = new Map<number, (typeof all)[number]>();
    for (const e of all) {
      employeeById.set(e.id, e);
    }

    const map = new Map<number, string>();
    for (const e of all) {
      if (e.managerId != null) {
        const manager = employeeById.get(e.managerId);
        const managerPositionCode =
          manager?.jobPositionId != null ? jobPositionCodeById.get(manager.jobPositionId) : undefined;
        map.set(e.id, managerPositionCode ?? "—");
      } else {
        const ownPositionCode =
          e.jobPositionId != null ? jobPositionCodeById.get(e.jobPositionId) : undefined;
        map.set(e.id, ownPositionCode ?? "—");
      }
    }
    return map;
  }, [data, jobPositionCodeById]);
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
  const [provisioningEmployee, setProvisioningEmployee] = useState<Employee | null>(null);
  const [provisioningResult, setProvisioningResult] = useState<ProvisionEmployeeUserResponse | null>(null);
  const [wizardStep, setWizardStep] = useState<"password" | "roles">("password");
  const [selectedRoleIds, setSelectedRoleIds] = useState<Set<number>>(new Set());

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

  useEffect(() => {
    if (provisioningResult) {
      setSelectedRoleIds(new Set(provisioningResult.assignedRoleIds));
      setWizardStep(provisioningResult.temporaryPassword ? "password" : "roles");
    }
  }, [provisioningResult]);

  const closeProvisioningModal = () => {
    setProvisioningEmployee(null);
    setProvisioningResult(null);
    setWizardStep("password");
    setSelectedRoleIds(new Set());
  };

  const handleEmployeeSuccess = (employee: Employee, mode: "create" | "edit") => {
    refetchList();

    if (mode !== "create") {
      return;
    }

    setProvisioningEmployee(employee);
    provisionMutation.mutate(employee.id, {
      onSuccess: (result) => {
        setProvisioningResult(result);
      },
      onError: (mutationError) => {
        toast.error(getErrorMessage(mutationError));
        closeProvisioningModal();
      },
    });
  };

  const toggleRole = (roleId: number, checked: boolean) => {
    setSelectedRoleIds((prev) => {
      const next = new Set(prev);
      if (checked) {
        next.add(roleId);
      } else {
        next.delete(roleId);
      }
      return next;
    });
  };

  const handleSaveRoles = async () => {
    if (!provisioningEmployee) return;

    try {
      await assignRolesMutation.mutateAsync({
        employeeId: provisioningEmployee.id,
        roleIds: Array.from(selectedRoleIds),
      });
      toast.success("Roles assigned.");
      closeProvisioningModal();
    } catch (mutationError) {
      toast.error(getErrorMessage(mutationError));
    }
  };

  const availableRoles = rolesQuery.data ?? [];
  const provisioningOpen = provisioningEmployee != null;
  const provisioningBusy = provisionMutation.isPending || assignRolesMutation.isPending;

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
          immediateManagerPositionByEmployeeId={immediateManagerPositionByEmployeeId}
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
            onSuccess={handleEmployeeSuccess}
            onCancel={closeForm}
          />
        ) : null}
      </Modal>

      <Modal
        open={provisioningOpen}
        title={wizardStep === "roles" ? "Assign roles" : "Account created"}
        onClose={closeProvisioningModal}
        className="max-w-xl"
        footer={
          wizardStep === "roles" ? (
            <>
              <Button type="button" variant="secondary" onClick={closeProvisioningModal}>
                Cancel
              </Button>
              <Button type="button" onClick={() => void handleSaveRoles()} disabled={provisioningBusy}>
                {assignRolesMutation.isPending ? "Saving…" : "Save roles"}
              </Button>
            </>
          ) : undefined
        }
      >
        {provisionMutation.isPending || !provisioningResult ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : wizardStep === "password" ? (
          <div className="space-y-4">
            <div className="rounded-lg border border-zinc-200 bg-zinc-50 p-4 dark:border-zinc-700 dark:bg-zinc-900/40">
              <p className="text-sm text-zinc-700 dark:text-zinc-200">
                Login account created for <span className="font-medium">{provisioningResult.employeeName}</span>.
              </p>
              <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">{provisioningResult.email}</p>
            </div>
            {provisioningResult.temporaryPassword ? (
              <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 dark:border-amber-900 dark:bg-amber-950/40">
                <p className="text-sm font-medium text-amber-900 dark:text-amber-100">Temporary password</p>
                <p className="mt-2 rounded-md bg-white px-3 py-2 font-mono text-sm text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
                  {provisioningResult.temporaryPassword}
                </p>
                <p className="mt-2 text-xs text-amber-800 dark:text-amber-200">
                  This password is shown once. Ask the employee to change it after first sign-in.
                </p>
              </div>
            ) : (
              <div className="rounded-lg border border-zinc-200 bg-zinc-50 p-4 text-sm text-zinc-700 dark:border-zinc-700 dark:bg-zinc-900/40 dark:text-zinc-200">
                An account already exists for this email. Continue to review and assign roles.
              </div>
            )}
            <div className="flex justify-end gap-2">
              <Button type="button" variant="secondary" onClick={closeProvisioningModal}>
                Close
              </Button>
              <Button type="button" onClick={() => setWizardStep("roles")}>
                Continue to roles
              </Button>
            </div>
          </div>
        ) : rolesQuery.isLoading ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : rolesQuery.isError ? (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
            {getErrorMessage(rolesQuery.error)}
          </div>
        ) : (
          <div className="space-y-4">
            <div className="rounded-lg border border-zinc-200 bg-zinc-50 p-4 text-sm text-zinc-700 dark:border-zinc-700 dark:bg-zinc-900/40 dark:text-zinc-200">
              Assign roles for <span className="font-medium">{provisioningResult.employeeName}</span>.
            </div>
            <ul className="max-h-[50vh] space-y-2 overflow-y-auto">
              {availableRoles.map((role: RoleDto) => (
                <li key={role.id}>
                  <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-zinc-200 px-3 py-2 dark:border-zinc-700">
                    <input
                      type="checkbox"
                      className="mt-0.5 size-4 rounded border-zinc-300"
                      checked={selectedRoleIds.has(role.id)}
                      onChange={(e) => toggleRole(role.id, e.target.checked)}
                    />
                    <span>
                      <span className="text-sm font-medium text-zinc-900 dark:text-zinc-100">{role.name}</span>
                      {role.description ? (
                        <span className="mt-0.5 block text-xs text-zinc-500">{role.description}</span>
                      ) : null}
                      {role.isSystem ? (
                        <span
                          className={cn(
                            "mt-0.5 block text-xs",
                            "text-amber-700 dark:text-amber-400",
                          )}
                        >
                          System role
                        </span>
                      ) : null}
                    </span>
                  </label>
                </li>
              ))}
            </ul>
          </div>
        )}
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

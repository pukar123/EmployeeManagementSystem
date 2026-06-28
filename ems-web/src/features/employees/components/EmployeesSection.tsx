"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useJobPositions } from "@/features/job-positions/hooks";
import type { JobPosition } from "@/features/job-positions/types/job-position.types";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { PageHeader } from "@/shared/components/PageHeader";
import { SearchInput } from "@/shared/components/SearchInput";
import { Spinner } from "@/shared/components/Spinner";
import { cn } from "@/shared/utils/cn";
import { employeePortalKeys } from "@/features/employee-portal/services/query-keys";
import { EmployeeTable } from "./EmployeeTable";
import { EmployeeHistoryModal } from "./EmployeeHistoryModal";
import { EmployeeForm } from "./EmployeeForm";
import { DeleteEmployeeDialog } from "./DeleteEmployeeDialog";
import { getErrorMessage } from "@/shared/api/http-client";
import { fetchRoles } from "@/features/user-management/services/userManagementApi";
import type { RoleDto } from "@/features/user-management/types";
import {
  useAssignEmployeeUserRoles,
  useEmployeeEffectiveRoles,
  useEmployees,
  useProvisionEmployeeUser,
  useSetEmployeeDirectRoles,
} from "../hooks";
import { employeeKeys } from "../services/query-keys";
import { employeeService } from "../services/employeeService";
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
  const setEmployeeDirectRolesMutation = useSetEmployeeDirectRoles();

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
  const [historyEmployee, setHistoryEmployee] = useState<Employee | null>(null);
  const [rolesEmployee, setRolesEmployee] = useState<Employee | null>(null);
  const [selectedDirectRoleIds, setSelectedDirectRoleIds] = useState<Set<number>>(new Set());
  const employeeRolesQuery = useEmployeeEffectiveRoles(rolesEmployee?.id ?? null);
  const historyQuery = useQuery({
    queryKey: ["employees", "history", historyEmployee?.id],
    queryFn: () => employeeService.getEmployeeHistory(historyEmployee!.id),
    enabled: historyEmployee != null,
  });

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

  const closeRolesModal = () => {
    setRolesEmployee(null);
    setSelectedDirectRoleIds(new Set());
  };

  const openRolesModal = (employee: Employee) => {
    setRolesEmployee(employee);
  };

  const closeProvisioningModal = () => {
    setProvisioningEmployee(null);
    setProvisioningResult(null);
    setWizardStep("password");
    setSelectedRoleIds(new Set());
  };

  const startProvisioning = (employee: Employee) => {
    setProvisioningEmployee(employee);
    provisionMutation.mutate(employee.id, {
      onSuccess: (result) => {
        void queryClient.invalidateQueries({ queryKey: employeeKeys.list() });
        void queryClient.invalidateQueries({ queryKey: employeePortalKeys.summary() });
        void queryClient.refetchQueries({ queryKey: employeePortalKeys.summary(), type: "all" });
        setProvisioningResult(result);
        setSelectedRoleIds(new Set(result.assignedRoleIds));
        setWizardStep(result.temporaryPassword ? "password" : "roles");
      },
      onError: (mutationError) => {
        toast.error(getErrorMessage(mutationError));
        closeProvisioningModal();
      },
    });
  };

  const handleEmployeeSuccess = (employee: Employee, mode: "create" | "edit") => {
    refetchList();

    if (mode !== "create") {
      return;
    }

    startProvisioning(employee);
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
      void queryClient.invalidateQueries({ queryKey: employeeKeys.list() });
      void queryClient.invalidateQueries({ queryKey: employeePortalKeys.summary() });
      void queryClient.refetchQueries({ queryKey: employeePortalKeys.summary(), type: "all" });
      closeProvisioningModal();
    } catch (mutationError) {
      toast.error(getErrorMessage(mutationError));
    }
  };

  const availableRoles = rolesQuery.data ?? [];
  const effectiveRoles = useMemo(() => employeeRolesQuery.data ?? [], [employeeRolesQuery.data]);
  const inheritedRoleIds = useMemo(
    () =>
      new Set(
        effectiveRoles
          .filter((role) => role.source === "position_inherited")
          .map((role) => role.roleId),
      ),
    [effectiveRoles],
  );
  const directRoleIds = useMemo(
    () =>
      new Set(
        effectiveRoles
          .filter((role) => role.source === "direct_override")
          .map((role) => role.roleId),
      ),
    [effectiveRoles],
  );

  const displayDirectRoleIds = selectedDirectRoleIds.size > 0 ? selectedDirectRoleIds : directRoleIds;

  const toggleDirectRole = (roleId: number, checked: boolean) => {
    setSelectedDirectRoleIds((prev) => {
      const next = new Set(prev.size > 0 ? prev : directRoleIds);
      if (checked) {
        next.add(roleId);
      } else {
        next.delete(roleId);
      }
      return next;
    });
  };

  const handleSaveDirectRoles = async () => {
    if (!rolesEmployee) return;

    const nextRoleIds = Array.from(displayDirectRoleIds);
    const removingRoles = Array.from(directRoleIds).filter((roleId) => !displayDirectRoleIds.has(roleId));
    if (removingRoles.length > 0 && !window.confirm("Remove selected direct override role(s) for this employee?")) {
      return;
    }

    try {
      await setEmployeeDirectRolesMutation.mutateAsync({
        employeeId: rolesEmployee.id,
        roleIds: nextRoleIds,
      });
      toast.success("Employee direct roles updated.");
      closeRolesModal();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };
  const provisioningOpen = provisioningEmployee != null;
  const provisioningBusy = provisionMutation.isPending || assignRolesMutation.isPending;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Employees"
        description={
          <>
            View and manage your employee records.{" "}
            <Link href="/employee-transfers" className="font-medium text-primary underline-offset-4 hover:underline">
              Open transfer workspace
            </Link>
          </>
        }
        actions={
          <Button type="button" onClick={openCreateForm}>
            Add employee
          </Button>
        }
      />

      <SearchInput
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Name, email, or employee #"
        aria-label="Search employees"
      />

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
          onViewHistory={(e) => setHistoryEmployee(e)}
          onManageRoles={openRolesModal}
          onProvisionUser={startProvisioning}
          provisionBusy={provisionMutation.isPending}
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
            <div className="rounded-lg border border-border bg-muted/50 p-4 dark:border-border dark:bg-card/40">
              <p className="text-sm text-muted-foreground">
                Login account created for <span className="font-medium">{provisioningResult.employeeName}</span>.
              </p>
              <p className="mt-1 text-sm text-muted-foreground">{provisioningResult.email}</p>
            </div>
            {provisioningResult.temporaryPassword ? (
              <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 dark:border-amber-900 dark:bg-amber-950/40">
                <p className="text-sm font-medium text-amber-900 dark:text-amber-100">Temporary password</p>
                <p className="mt-2 rounded-md bg-card px-3 py-2 font-mono text-sm text-foreground">
                  {provisioningResult.temporaryPassword}
                </p>
                <p className="mt-2 text-xs text-amber-800 dark:text-amber-200">
                  This password is shown once. Ask the employee to change it after first sign-in.
                </p>
              </div>
            ) : (
              <div className="rounded-lg border border-border bg-muted/50 p-4 text-sm text-muted-foreground dark:border-border dark:bg-card/40 dark:text-foreground">
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
            <div className="rounded-lg border border-border bg-muted/50 p-4 text-sm text-muted-foreground dark:border-border dark:bg-card/40 dark:text-foreground">
              Assign roles for <span className="font-medium">{provisioningResult.employeeName}</span>.
            </div>
            <ul className="max-h-[50vh] space-y-2 overflow-y-auto">
              {availableRoles.map((role: RoleDto) => (
                <li key={role.id}>
                  <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-border px-3 py-2 dark:border-border">
                    <input
                      type="checkbox"
                      className="mt-0.5 size-4 rounded border-input"
                      checked={selectedRoleIds.has(role.id)}
                      onChange={(e) => toggleRole(role.id, e.target.checked)}
                    />
                    <span>
                      <span className="text-sm font-medium text-foreground">{role.name}</span>
                      {role.description ? (
                        <span className="mt-0.5 block text-xs text-muted-foreground">{role.description}</span>
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

      <EmployeeHistoryModal
        open={historyEmployee != null}
        employee={historyEmployee}
        history={historyQuery.data}
        isLoading={historyQuery.isLoading || historyQuery.isFetching}
        isError={historyQuery.isError}
        errorMessage={historyQuery.isError ? getErrorMessage(historyQuery.error) : null}
        onClose={() => setHistoryEmployee(null)}
      />

      <Modal
        open={rolesEmployee != null}
        title={rolesEmployee ? `Employee roles: ${rolesEmployee.firstName} ${rolesEmployee.lastName}` : "Employee roles"}
        onClose={closeRolesModal}
        className="max-w-2xl"
        footer={
          <>
            <Button type="button" variant="secondary" onClick={closeRolesModal}>
              Close
            </Button>
            <Button type="button" onClick={() => void handleSaveDirectRoles()} disabled={setEmployeeDirectRolesMutation.isPending}>
              {setEmployeeDirectRolesMutation.isPending ? "Saving…" : "Save direct overrides"}
            </Button>
          </>
        }
      >
        {employeeRolesQuery.isLoading ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : employeeRolesQuery.isError ? (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
            {getErrorMessage(employeeRolesQuery.error)}
          </div>
        ) : (
          <div className="space-y-4">
            <div className="rounded-lg border border-border bg-muted/50 p-4 text-sm text-muted-foreground dark:border-border dark:bg-card/40 dark:text-foreground">
              Effective roles are inherited from position plus direct overrides. Position-inherited roles are read-only.
            </div>
            <div className="space-y-2">
              {availableRoles.map((role) => {
                const inherited = inheritedRoleIds.has(role.id);
                const checked = inherited || displayDirectRoleIds.has(role.id);
                return (
                  <label
                    key={role.id}
                    className="flex items-start gap-3 rounded-lg border border-border px-3 py-2 dark:border-border"
                  >
                    <input
                      type="checkbox"
                      className="mt-0.5 size-4 rounded border-input"
                      checked={checked}
                      disabled={inherited}
                      onChange={(e) => toggleDirectRole(role.id, e.target.checked)}
                    />
                    <span className="text-sm">
                      <span className="font-medium text-foreground">{role.name}</span>
                      <span className="mt-0.5 block text-xs text-muted-foreground">
                        {inherited ? "Source: position_inherited" : checked ? "Source: direct_override" : "Not assigned"}
                      </span>
                    </span>
                  </label>
                );
              })}
            </div>
            {effectiveRoles.length > 0 ? (
              <div className="rounded-lg border border-border p-3 dark:border-border">
                <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">Current effective roles</p>
                <ul className="space-y-1 text-sm text-muted-foreground">
                  {effectiveRoles.map((role) => (
                    <li key={`${role.roleId}-${role.source}-${role.jobPositionId ?? "none"}`}>
                      {role.roleName}
                      {" - "}
                      {role.source === "position_inherited"
                        ? `position_inherited (${role.jobPositionTitle ?? "position"})`
                        : "direct_override"}
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
          </div>
        )}
      </Modal>
    </div>
  );
}

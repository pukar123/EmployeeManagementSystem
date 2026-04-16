"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo } from "react";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import { useDepartments } from "@/features/departments/hooks";
import { useJobPositions } from "@/features/job-positions/hooks";
import type { Department } from "@/features/departments/types/department.types";
import type { JobPosition } from "@/features/job-positions/types/job-position.types";
import { useLocations } from "@/features/locations/hooks";
import type { Location } from "@/features/locations/types/location.types";
import { Button } from "@/shared/components/Button";
import { SearchableSelect } from "@/shared/components/SearchableSelect";
import { getErrorMessage } from "@/shared/api/http-client";
import { nullableNumberToStringId, stringIdFieldToNullableNumber } from "@/shared/utils/optional-id-string";
import { toSelectOptions } from "@/shared/utils/to-select-options";
import { useCreateEmployee, useEmployees, useUpdateEmployee } from "../hooks";
import {
  employeeFormSchema,
  type EmployeeFormInput,
  type EmployeeFormValues,
} from "../types/employee-form.schema";
import type { Employee } from "../types/employee.types";
import { employmentStatusLabels } from "../types/employment-status";
import { dateInputToApiIso, toDateInputValue } from "../utils/date-format";
import type { CreateEmployeeRequest, UpdateEmployeeRequest } from "../types/employee.types";
import { useEmployeeUiStore } from "../store/employee-ui-store";
import { useOrganizationContext } from "@/providers/OrganizationProvider";

const inputClass =
  "mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100";

function formatDepartmentLabel(d: Department): string {
  return d.code ? `${d.name} (${d.code})` : d.name;
}

function formatLocationLabel(l: Location): string {
  return l.city ? `${l.name} — ${l.city}` : l.name;
}

function formatJobLabel(j: JobPosition): string {
  return j.code ? `${j.title} (${j.code})` : j.title;
}

function emptyDefaults(organizationId: number): EmployeeFormInput {
  return {
    organizationId,
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    dateOfBirth: "",
    dateJoined: "",
    employmentStatus: 0,
    departmentId: "",
    locationId: "",
    managerId: "",
    jobPositionId: "",
  };
}

function employeeToFormInput(emp: Employee): EmployeeFormInput {
  const idStr = (n: number | null) => (n != null ? String(n) : "");
  return {
    organizationId: emp.organizationId,
    firstName: emp.firstName,
    lastName: emp.lastName,
    email: emp.email,
    phoneNumber: emp.phoneNumber ?? "",
    dateOfBirth: toDateInputValue(emp.dateOfBirth),
    dateJoined: toDateInputValue(emp.dateJoined),
    employmentStatus: emp.employmentStatus,
    departmentId: idStr(emp.departmentId),
    locationId: idStr(emp.locationId),
    managerId: idStr(emp.managerId),
    jobPositionId: idStr(emp.jobPositionId),
  };
}

function toApiPayload(values: EmployeeFormValues): CreateEmployeeRequest {
  return {
    organizationId: values.organizationId,
    departmentId: values.departmentId,
    locationId: values.locationId,
    managerId: values.managerId,
    jobPositionId: values.jobPositionId,
    firstName: values.firstName,
    lastName: values.lastName,
    email: values.email,
    phoneNumber: values.phoneNumber?.trim() ? values.phoneNumber.trim() : null,
    dateOfBirth: dateInputToApiIso(values.dateOfBirth),
    dateJoined: dateInputToApiIso(values.dateJoined),
    employmentStatus: values.employmentStatus,
  };
}

type EmployeeFormProps = {
  mode: "create" | "edit";
  employee: Employee | null;
  onSuccess: (employee: Employee, mode: "create" | "edit") => void;
  onCancel: () => void;
};

export function EmployeeForm({ mode, employee, onSuccess, onCancel }: EmployeeFormProps) {
  const { organizationId: currentOrgId } = useOrganizationContext();
  const createMutation = useCreateEmployee();
  const updateMutation = useUpdateEmployee();
  const closeForm = useEmployeeUiStore((s) => s.closeForm);

  const { data: departments = [], isLoading: departmentsLoading } = useDepartments();
  const { data: locations = [], isLoading: locationsLoading } = useLocations();
  const { data: employees = [], isLoading: employeesLoading } = useEmployees();
  const { data: jobPositions = [], isLoading: jobPositionsLoading } = useJobPositions(currentOrgId);

  const departmentOptions = useMemo(
    () =>
      toSelectOptions(
        departments.filter((d) => d.organizationId === currentOrgId),
        (d) => d.id,
        formatDepartmentLabel,
      ),
    [departments, currentOrgId],
  );

  const locationOptions = useMemo(
    () =>
      toSelectOptions(
        locations.filter((l) => l.organizationId === currentOrgId),
        (l) => l.id,
        formatLocationLabel,
      ),
    [locations, currentOrgId],
  );

  const managerCandidates = useMemo(() => {
    return employees.filter((e) => {
      if (e.organizationId !== currentOrgId) return false;
      if (mode === "edit" && employee && e.id === employee.id) return false;
      return true;
    });
  }, [employees, currentOrgId, mode, employee]);

  const managerOptions = useMemo(
    () =>
      toSelectOptions(managerCandidates, (e) => e.id, (e) =>
        `${e.firstName} ${e.lastName} (${e.employeeNumber})`,
      ),
    [managerCandidates],
  );

  const jobOptions = useMemo(
    () => toSelectOptions(jobPositions, (j) => j.id, formatJobLabel),
    [jobPositions],
  );

  const form = useForm<EmployeeFormInput, unknown, EmployeeFormValues>({
    resolver: zodResolver(employeeFormSchema),
    defaultValues:
      currentOrgId != null ? emptyDefaults(currentOrgId) : emptyDefaults(1),
  });

  useEffect(() => {
    if (currentOrgId == null) return;
    if (mode === "edit" && employee) {
      form.reset(employeeToFormInput(employee));
    } else {
      form.reset(emptyDefaults(currentOrgId));
    }
  }, [mode, employee, currentOrgId, form.reset]);

  const onSubmit = (values: EmployeeFormValues) => {
    if (currentOrgId == null) {
      toast.error("Organization is not loaded.");
      return;
    }
    const payload = {
      ...toApiPayload(values),
      organizationId: currentOrgId,
    } as UpdateEmployeeRequest;

    if (mode === "create") {
      createMutation.mutate(payload as CreateEmployeeRequest, {
        onSuccess: (createdEmployee) => {
          toast.success("Employee created");
          closeForm();
          onSuccess(createdEmployee, "create");
        },
        onError: (e) => toast.error(getErrorMessage(e)),
      });
    } else if (employee) {
      updateMutation.mutate(
        { id: employee.id, data: payload },
        {
          onSuccess: (updatedEmployee) => {
            toast.success("Employee updated");
            closeForm();
            onSuccess(updatedEmployee, "edit");
          },
          onError: (e) => toast.error(getErrorMessage(e)),
        },
      );
    }
  };

  const pending = createMutation.isPending || updateMutation.isPending;

  if (currentOrgId == null) {
    return <p className="text-sm text-zinc-500">Loading organization…</p>;
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        {mode === "edit" && employee ? (
          <div className="sm:col-span-2">
            <p className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Employee number</p>
            <p className="mt-1 rounded-lg border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm text-zinc-800 dark:border-zinc-700 dark:bg-zinc-900/50 dark:text-zinc-100">
              {employee.employeeNumber}
            </p>
          </div>
        ) : (
          <div className="sm:col-span-2 rounded-lg border border-dashed border-zinc-300 bg-zinc-50/80 px-3 py-2 text-sm text-zinc-600 dark:border-zinc-600 dark:bg-zinc-900/30 dark:text-zinc-400">
            Employee number will be assigned automatically when you save (e.g. <span className="font-mono">EMP001</span>,{" "}
            <span className="font-mono">EMP002</span>, …).
          </div>
        )}
        <Field label="First name" error={form.formState.errors.firstName?.message}>
          <input type="text" className={inputClass} {...form.register("firstName")} />
        </Field>
        <Field label="Last name" error={form.formState.errors.lastName?.message}>
          <input type="text" className={inputClass} {...form.register("lastName")} />
        </Field>
        <Field label="Email" error={form.formState.errors.email?.message}>
          <input type="email" className={inputClass} {...form.register("email")} />
        </Field>
        <Field label="Phone" error={form.formState.errors.phoneNumber?.message}>
          <input type="text" className={inputClass} {...form.register("phoneNumber")} />
        </Field>
        <Field label="Date of birth" error={form.formState.errors.dateOfBirth?.message}>
          <input type="date" className={inputClass} {...form.register("dateOfBirth")} />
        </Field>
        <Field label="Date joined" error={form.formState.errors.dateJoined?.message}>
          <input type="date" className={inputClass} {...form.register("dateJoined")} />
        </Field>
        <Field label="Employment status" error={form.formState.errors.employmentStatus?.message}>
          <select className={inputClass} {...form.register("employmentStatus", { valueAsNumber: true })}>
            {Object.entries(employmentStatusLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Department (optional)" error={form.formState.errors.departmentId?.message}>
          <Controller
            name="departmentId"
            control={form.control}
            render={({ field }) => (
              <SearchableSelect<number>
                options={departmentOptions}
                value={stringIdFieldToNullableNumber(field.value)}
                onChange={(v) => field.onChange(nullableNumberToStringId(v))}
                placeholder="Search departments…"
                emptyLabel="No department"
                disabled={departmentsLoading}
                aria-invalid={!!form.formState.errors.departmentId}
              />
            )}
          />
        </Field>
        <Field label="Location (optional)" error={form.formState.errors.locationId?.message}>
          <Controller
            name="locationId"
            control={form.control}
            render={({ field }) => (
              <SearchableSelect<number>
                options={locationOptions}
                value={stringIdFieldToNullableNumber(field.value)}
                onChange={(v) => field.onChange(nullableNumberToStringId(v))}
                placeholder="Search locations…"
                emptyLabel="No location"
                disabled={locationsLoading}
                aria-invalid={!!form.formState.errors.locationId}
              />
            )}
          />
        </Field>
        <Field label="Manager (optional)" error={form.formState.errors.managerId?.message}>
          <Controller
            name="managerId"
            control={form.control}
            render={({ field }) => (
              <SearchableSelect<number>
                options={managerOptions}
                value={stringIdFieldToNullableNumber(field.value)}
                onChange={(v) => field.onChange(nullableNumberToStringId(v))}
                placeholder="Search employees…"
                emptyLabel="No manager"
                disabled={employeesLoading}
                aria-invalid={!!form.formState.errors.managerId}
              />
            )}
          />
        </Field>
        <Field label="Job position (optional)" error={form.formState.errors.jobPositionId?.message}>
          <Controller
            name="jobPositionId"
            control={form.control}
            render={({ field }) => (
              <SearchableSelect<number>
                options={jobOptions}
                value={stringIdFieldToNullableNumber(field.value)}
                onChange={(v) => field.onChange(nullableNumberToStringId(v))}
                placeholder="Search job positions…"
                emptyLabel="No job position"
                disabled={jobPositionsLoading}
                aria-invalid={!!form.formState.errors.jobPositionId}
              />
            )}
          />
        </Field>
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onCancel} disabled={pending}>
          Cancel
        </Button>
        <Button type="submit" disabled={pending}>
          {pending ? "Saving…" : mode === "create" ? "Create" : "Save changes"}
        </Button>
      </div>
    </form>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: import("react").ReactNode;
}) {
  return (
    <div className="sm:col-span-1">
      <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
        {label}
      </label>
      {children}
      {error ? <p className="mt-1 text-xs text-red-600 dark:text-red-400">{error}</p> : null}
    </div>
  );
}

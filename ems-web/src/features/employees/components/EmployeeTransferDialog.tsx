"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { Controller, useForm, type Resolver } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { useDepartments } from "@/features/departments/hooks";
import { useJobPositions } from "@/features/job-positions/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { SearchableSelect } from "@/shared/components/SearchableSelect";
import { getErrorMessage } from "@/shared/api/http-client";
import { cn } from "@/shared/utils/cn";
import { toSelectOptions } from "@/shared/utils/to-select-options";
import { useEmployees } from "../hooks";
import { employeeService } from "../services/employeeService";
import {
  departmentTransferSchema,
  positionTransferSchema,
  type DepartmentTransferValues,
  type PositionTransferValues,
} from "../types/employee-transfer.schema";
import type { EmployeeProfile } from "../types/employee.types";
import { EmploymentStatus } from "../types/employment-status";

const managerTransferSchema = z.object({
  newManagerId: z.number().int().positive().nullable().optional(),
  effectiveFromUtc: z.string().min(1, "Effective date is required"),
  reason: z.string().trim().max(500, "Reason is too long").optional(),
});

type ManagerTransferValues = z.infer<typeof managerTransferSchema>;

type TransferKind = "department" | "position" | "manager";

type EmployeeTransferDialogProps = {
  open: boolean;
  onClose: () => void;
  profile: EmployeeProfile;
  onTransferred: () => void;
};

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

const transferKindLabels: Record<TransferKind, string> = {
  department: "Department",
  position: "Position",
  manager: "Manager",
};

function formatEmployeeLabel(e: { firstName: string; lastName: string; employeeNumber: string }): string {
  return `${e.firstName} ${e.lastName} (${e.employeeNumber})`;
}

function labelOrDash(value: string | null | undefined): string {
  return value?.trim() ? value : "—";
}

export function EmployeeTransferDialog({ open, onClose, profile, onTransferred }: EmployeeTransferDialogProps) {
  const { organizationId } = useOrganizationContext();
  const [kind, setKind] = useState<TransferKind>("department");
  const [confirming, setConfirming] = useState(false);

  const { data: departments = [], isLoading: departmentsLoading } = useDepartments();
  const { data: positions = [], isLoading: positionsLoading } = useJobPositions(organizationId);
  const { data: employees = [], isLoading: employeesLoading } = useEmployees();

  const departmentForm = useForm<DepartmentTransferValues>({
    resolver: zodResolver(departmentTransferSchema) as Resolver<DepartmentTransferValues>,
    defaultValues: {
      newDepartmentId: profile.departmentId ?? undefined,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    },
  });

  const positionForm = useForm<PositionTransferValues>({
    resolver: zodResolver(positionTransferSchema) as Resolver<PositionTransferValues>,
    defaultValues: {
      newJobPositionId: profile.jobPositionId ?? undefined,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    },
  });

  const managerForm = useForm<ManagerTransferValues>({
    resolver: zodResolver(managerTransferSchema) as Resolver<ManagerTransferValues>,
    defaultValues: {
      newManagerId: profile.managerId ?? null,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    },
  });

  useEffect(() => {
    if (!open) return;
    setKind("department");
    setConfirming(false);
    departmentForm.reset({
      newDepartmentId: profile.departmentId ?? undefined,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    });
    positionForm.reset({
      newJobPositionId: profile.jobPositionId ?? undefined,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    });
    managerForm.reset({
      newManagerId: profile.managerId ?? null,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    });
  }, [open, profile, departmentForm, positionForm, managerForm]);

  const transferMutation = useMutation({
    mutationFn: async (args: { kind: TransferKind; values: DepartmentTransferValues | PositionTransferValues | ManagerTransferValues }) => {
      const effectiveFromUtc = new Date(args.values.effectiveFromUtc).toISOString();
      const reason = args.values.reason?.trim() || null;

      if (args.kind === "department") {
        const values = args.values as DepartmentTransferValues;
        return employeeService.transferDepartment(profile.id, {
          newDepartmentId: values.newDepartmentId ?? null,
          effectiveFromUtc,
          reason,
        });
      }
      if (args.kind === "position") {
        const values = args.values as PositionTransferValues;
        return employeeService.transferPosition(profile.id, {
          newJobPositionId: values.newJobPositionId ?? null,
          effectiveFromUtc,
          reason,
        });
      }
      const values = args.values as ManagerTransferValues;
      return employeeService.transferManager(profile.id, {
        newManagerId: values.newManagerId ?? null,
        effectiveFromUtc,
        reason,
      });
    },
    onSuccess: () => {
      toast.success(`${transferKindLabels[kind]} transfer saved.`);
      onTransferred();
      onClose();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const departmentOptions = useMemo(
    () =>
      toSelectOptions(
        departments.filter((d) => d.organizationId === profile.organizationId),
        (d) => d.id,
        (d) => (d.code ? `${d.name} (${d.code})` : d.name),
      ),
    [departments, profile.organizationId],
  );

  const positionOptions = useMemo(
    () =>
      toSelectOptions(
        positions.filter((p) => p.organizationId === profile.organizationId),
        (p) => p.id,
        (p) => (p.code ? `${p.title} (${p.code})` : p.title),
      ),
    [positions, profile.organizationId],
  );

  const managerOptions = useMemo(
    () =>
      toSelectOptions(
        employees.filter((e) => {
          if (e.organizationId !== profile.organizationId) return false;
          if (e.isArchived || !e.isActive || e.employmentStatus !== EmploymentStatus.Active) return false;
          if (e.id === profile.id) return false;
          return true;
        }),
        (e) => e.id,
        formatEmployeeLabel,
      ),
    [employees, profile],
  );

  const activeForm = kind === "department" ? departmentForm : kind === "position" ? positionForm : managerForm;

  const currentLabel =
    kind === "department"
      ? labelOrDash(profile.departmentName)
      : kind === "position"
        ? labelOrDash(profile.jobPositionTitle)
        : profile.managerName
          ? `${profile.managerName}${profile.managerEmployeeNumber ? ` (${profile.managerEmployeeNumber})` : ""}`
          : "No manager";

  const watchedValues = activeForm.watch();
  const newLabel = useMemo(() => {
    if (kind === "department") {
      const id = (watchedValues as DepartmentTransferValues).newDepartmentId;
      const dept = departments.find((d) => d.id === id);
      return dept ? (dept.code ? `${dept.name} (${dept.code})` : dept.name) : "—";
    }
    if (kind === "position") {
      const id = (watchedValues as PositionTransferValues).newJobPositionId;
      const pos = positions.find((p) => p.id === id);
      return pos ? (pos.code ? `${pos.title} (${pos.code})` : pos.title) : "—";
    }
    const id = (watchedValues as ManagerTransferValues).newManagerId;
    if (id == null) return "No manager";
    const mgr = employees.find((e) => e.id === id);
    return mgr ? formatEmployeeLabel(mgr) : "—";
  }, [kind, watchedValues, departments, positions, employees]);

  const handleReview = () => {
    void activeForm.handleSubmit(() => setConfirming(true))();
  };

  const confirmValues =
    kind === "department"
      ? departmentForm.getValues()
      : kind === "position"
        ? positionForm.getValues()
        : managerForm.getValues();

  const handleConfirm = () => {
    transferMutation.mutate({ kind, values: confirmValues });
  };

  const handleClose = () => {
    if (transferMutation.isPending) return;
    onClose();
  };

  const optionsLoading =
    kind === "department" ? departmentsLoading : kind === "position" ? positionsLoading : employeesLoading;

  return (
    <Modal
      open={open}
      title={`Transfer ${profile.firstName} ${profile.lastName}`}
      onClose={handleClose}
      className="max-w-xl"
      footer={
        confirming ? (
          <>
            <Button type="button" variant="secondary" onClick={() => setConfirming(false)} disabled={transferMutation.isPending}>
              Back
            </Button>
            <Button type="button" onClick={handleConfirm} disabled={transferMutation.isPending}>
              {transferMutation.isPending ? "Transferring…" : "Confirm transfer"}
            </Button>
          </>
        ) : (
          <>
            <Button type="button" variant="secondary" onClick={handleClose}>
              Cancel
            </Button>
            <Button type="button" onClick={handleReview}>
              Review transfer
            </Button>
          </>
        )
      }
    >
      {!confirming ? (
        <div className="space-y-4">
          <div className="flex flex-wrap gap-2">
            {(["department", "position", "manager"] as TransferKind[]).map((value) => (
              <button
                key={value}
                type="button"
                onClick={() => setKind(value)}
                className={cn(
                  "rounded-full px-3 py-1 text-xs font-medium uppercase tracking-wide transition-colors",
                  kind === value
                    ? "bg-primary/10 text-primary"
                    : "bg-muted text-muted-foreground hover:text-foreground",
                )}
              >
                {transferKindLabels[value]}
              </button>
            ))}
          </div>

          {kind === "department" ? (
            <form className="space-y-4" onSubmit={(e) => e.preventDefault()}>
              <p className="text-xs text-muted-foreground">
                Current department: <span className="font-medium text-foreground">{currentLabel}</span>
              </p>
              <div>
                <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  New department
                </label>
                <Controller
                  name="newDepartmentId"
                  control={departmentForm.control}
                  render={({ field }) => (
                    <SearchableSelect<number>
                      options={departmentOptions}
                      value={field.value ?? null}
                      onChange={(v) => field.onChange(v ?? undefined)}
                      placeholder="Search departments…"
                      emptyLabel="No department"
                      disabled={optionsLoading}
                    />
                  )}
                />
                {departmentForm.formState.errors.newDepartmentId ? (
                  <p className="mt-1 text-xs text-red-600">{departmentForm.formState.errors.newDepartmentId.message}</p>
                ) : null}
              </div>
              <Field label="Effective date" error={departmentForm.formState.errors.effectiveFromUtc?.message}>
                <input type="date" className={inputClass} {...departmentForm.register("effectiveFromUtc")} />
              </Field>
              <Field label="Reason (optional)" error={departmentForm.formState.errors.reason?.message}>
                <textarea className={inputClass} rows={3} {...departmentForm.register("reason")} />
              </Field>
            </form>
          ) : null}

          {kind === "position" ? (
            <form className="space-y-4" onSubmit={(e) => e.preventDefault()}>
              <p className="text-xs text-muted-foreground">
                Current position: <span className="font-medium text-foreground">{currentLabel}</span>
              </p>
              <div>
                <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  New position
                </label>
                <Controller
                  name="newJobPositionId"
                  control={positionForm.control}
                  render={({ field }) => (
                    <SearchableSelect<number>
                      options={positionOptions}
                      value={field.value ?? null}
                      onChange={(v) => field.onChange(v ?? undefined)}
                      placeholder="Search positions…"
                      emptyLabel="No position"
                      disabled={optionsLoading}
                    />
                  )}
                />
                {positionForm.formState.errors.newJobPositionId ? (
                  <p className="mt-1 text-xs text-red-600">{positionForm.formState.errors.newJobPositionId.message}</p>
                ) : null}
              </div>
              <Field label="Effective date" error={positionForm.formState.errors.effectiveFromUtc?.message}>
                <input type="date" className={inputClass} {...positionForm.register("effectiveFromUtc")} />
              </Field>
              <Field label="Reason (optional)" error={positionForm.formState.errors.reason?.message}>
                <textarea className={inputClass} rows={3} {...positionForm.register("reason")} />
              </Field>
            </form>
          ) : null}

          {kind === "manager" ? (
            <form className="space-y-4" onSubmit={(e) => e.preventDefault()}>
              <p className="text-xs text-muted-foreground">
                Current manager: <span className="font-medium text-foreground">{currentLabel}</span>
              </p>
              <div>
                <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  New manager
                </label>
                <Controller
                  name="newManagerId"
                  control={managerForm.control}
                  render={({ field }) => (
                    <SearchableSelect<number>
                      options={managerOptions}
                      value={field.value ?? null}
                      onChange={(v) => field.onChange(v)}
                      placeholder="Search employees…"
                      emptyLabel="No manager"
                      disabled={optionsLoading}
                    />
                  )}
                />
              </div>
              <Field label="Effective date" error={managerForm.formState.errors.effectiveFromUtc?.message}>
                <input type="date" className={inputClass} {...managerForm.register("effectiveFromUtc")} />
              </Field>
              <Field label="Reason (optional)" error={managerForm.formState.errors.reason?.message}>
                <textarea className={inputClass} rows={3} {...managerForm.register("reason")} />
              </Field>
            </form>
          ) : null}
        </div>
      ) : (
        <div className="space-y-4 rounded-lg border border-amber-500/40 bg-amber-500/10 p-4">
          <p className="text-sm font-medium text-foreground">Confirm {transferKindLabels[kind].toLowerCase()} transfer</p>
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground">Current</dt>
              <dd className="font-medium text-foreground">{currentLabel}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground">New</dt>
              <dd className="font-medium text-foreground">{newLabel}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground">Effective date</dt>
              <dd className="font-medium text-foreground">{confirmValues.effectiveFromUtc}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground">Reason</dt>
              <dd className="font-medium text-foreground">{confirmValues.reason?.trim() || "—"}</dd>
            </div>
          </dl>
        </div>
      )}
    </Modal>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</label>
      {children}
      {error ? <p className="mt-1 text-xs text-red-600">{error}</p> : null}
    </div>
  );
}

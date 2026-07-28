"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm, type Resolver } from "react-hook-form";
import { SearchableSelect } from "@/shared/components/SearchableSelect";
import { Button } from "@/shared/components/Button";
import { toSelectOptions } from "@/shared/utils/to-select-options";
import type { Department } from "@/features/departments/types/department.types";
import type { Employee } from "../types/employee.types";
import {
  departmentTransferSchema,
  type DepartmentTransferValues,
} from "../types/employee-transfer.schema";

type DepartmentTransferFormProps = {
  employee: Employee;
  departments: Department[];
  loadingOptions: boolean;
  saving: boolean;
  onSubmit: (values: DepartmentTransferValues) => Promise<void>;
};

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

function formatDepartmentLabel(d: Department): string {
  return d.code ? `${d.name} (${d.code})` : d.name;
}

export function DepartmentTransferForm({
  employee,
  departments,
  loadingOptions,
  saving,
  onSubmit,
}: DepartmentTransferFormProps) {
  const form = useForm<DepartmentTransferValues>({
    resolver: zodResolver(departmentTransferSchema) as Resolver<DepartmentTransferValues>,
    defaultValues: {
      newDepartmentId: employee.departmentId ?? undefined,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    },
  });

  const options = toSelectOptions(
    departments.filter((d) => d.organizationId === employee.organizationId),
    (d) => d.id,
    formatDepartmentLabel,
  );

  return (
    <form
      className="space-y-4 rounded-lg border border-border p-4 dark:border-border"
      onSubmit={form.handleSubmit(async (values) => onSubmit(values))}
    >
      <h3 className="text-sm font-semibold text-foreground">Department Transfer</h3>
      <p className="text-xs text-muted-foreground">Current department ID: {employee.departmentId ?? "—"}</p>

      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">New department</label>
        <Controller
          name="newDepartmentId"
          control={form.control}
          render={({ field }) => (
            <SearchableSelect<number>
              options={options}
              value={field.value ?? null}
              onChange={(v) => field.onChange(v ?? undefined)}
              placeholder="Search departments…"
              emptyLabel="No department"
              disabled={loadingOptions}
              aria-invalid={!!form.formState.errors.newDepartmentId}
            />
          )}
        />
        {form.formState.errors.newDepartmentId ? (
          <p className="mt-1 text-xs text-red-600">{form.formState.errors.newDepartmentId.message}</p>
        ) : null}
      </div>

      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">Effective date</label>
        <input type="date" className={inputClass} {...form.register("effectiveFromUtc")} />
        {form.formState.errors.effectiveFromUtc ? (
          <p className="mt-1 text-xs text-red-600">{form.formState.errors.effectiveFromUtc.message}</p>
        ) : null}
      </div>

      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">Reason (optional)</label>
        <textarea className={inputClass} rows={3} {...form.register("reason")} />
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={saving}>
          {saving ? "Transferring…" : "Transfer department"}
        </Button>
      </div>
    </form>
  );
}

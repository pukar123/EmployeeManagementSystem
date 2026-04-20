"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { SearchableSelect } from "@/shared/components/SearchableSelect";
import { Button } from "@/shared/components/Button";
import { toSelectOptions } from "@/shared/utils/to-select-options";
import type { JobPosition } from "@/features/job-positions/types/job-position.types";
import type { Employee } from "../types/employee.types";
import {
  positionTransferSchema,
  type PositionTransferValues,
} from "../types/employee-transfer.schema";

type PositionTransferFormProps = {
  employee: Employee;
  positions: JobPosition[];
  loadingOptions: boolean;
  saving: boolean;
  onSubmit: (values: PositionTransferValues) => Promise<void>;
};

const inputClass =
  "mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100";

function formatPositionLabel(j: JobPosition): string {
  return j.code ? `${j.title} (${j.code})` : j.title;
}

export function PositionTransferForm({
  employee,
  positions,
  loadingOptions,
  saving,
  onSubmit,
}: PositionTransferFormProps) {
  const form = useForm<PositionTransferValues>({
    resolver: zodResolver(positionTransferSchema),
    defaultValues: {
      newJobPositionId: employee.jobPositionId ?? undefined,
      effectiveFromUtc: new Date().toISOString().slice(0, 10),
      reason: "",
    },
  });

  const options = toSelectOptions(
    positions.filter((j) => j.organizationId === employee.organizationId),
    (j) => j.id,
    formatPositionLabel,
  );

  return (
    <form
      className="space-y-4 rounded-lg border border-zinc-200 p-4 dark:border-zinc-700"
      onSubmit={form.handleSubmit(async (values) => onSubmit(values))}
    >
      <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Position Transfer</h3>
      <p className="text-xs text-zinc-500">Current position ID: {employee.jobPositionId ?? "—"}</p>

      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500">New position</label>
        <Controller
          name="newJobPositionId"
          control={form.control}
          render={({ field }) => (
            <SearchableSelect<number>
              options={options}
              value={field.value ?? null}
              onChange={(v) => field.onChange(v ?? undefined)}
              placeholder="Search positions…"
              emptyLabel="No position"
              disabled={loadingOptions}
              aria-invalid={!!form.formState.errors.newJobPositionId}
            />
          )}
        />
        {form.formState.errors.newJobPositionId ? (
          <p className="mt-1 text-xs text-red-600">{form.formState.errors.newJobPositionId.message}</p>
        ) : null}
      </div>

      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500">Effective date</label>
        <input type="date" className={inputClass} {...form.register("effectiveFromUtc")} />
        {form.formState.errors.effectiveFromUtc ? (
          <p className="mt-1 text-xs text-red-600">{form.formState.errors.effectiveFromUtc.message}</p>
        ) : null}
      </div>

      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500">Reason (optional)</label>
        <textarea className={inputClass} rows={3} {...form.register("reason")} />
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={saving}>
          {saving ? "Transferring…" : "Transfer position"}
        </Button>
      </div>
    </form>
  );
}

"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { useLeaveTypeMutations, useLeaveTypes } from "../hooks";
import type { LeaveType, LeaveUnitValue } from "../types/leave.types";

type FormState = {
  name: string;
  description: string;
  unit: LeaveUnitValue;
  requiresAttachment: boolean;
  isActive: boolean;
};

const defaultForm: FormState = {
  name: "",
  description: "",
  unit: 1,
  requiresAttachment: false,
  isActive: true,
};

function toFormState(leaveType: LeaveType): FormState {
  return {
    name: leaveType.name,
    description: leaveType.description ?? "",
    unit: leaveType.unit,
    requiresAttachment: leaveType.requiresAttachment,
    isActive: leaveType.isActive,
  };
}

function unitLabel(unit: LeaveUnitValue): string {
  return unit === 2 ? "Hours" : "Days";
}

export function LeaveSettingsSection() {
  const { organizationId } = useOrganizationContext();
  const leaveTypesQuery = useLeaveTypes(organizationId);
  const { createLeaveType, updateLeaveType, deleteLeaveType } = useLeaveTypeMutations(organizationId);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<FormState>(defaultForm);

  const selectedLeaveType = useMemo(() => {
    if (editingId == null) return null;
    return (leaveTypesQuery.data ?? []).find((row) => row.id === editingId) ?? null;
  }, [editingId, leaveTypesQuery.data]);

  const startCreate = () => {
    setEditingId(null);
    setForm(defaultForm);
  };

  const startEdit = (leaveType: LeaveType) => {
    setEditingId(leaveType.id);
    setForm(toFormState(leaveType));
  };

  const submit = async () => {
    if (!organizationId) return;
    if (!form.name.trim()) {
      toast.error("Leave type name is required.");
      return;
    }

    const payload = {
      organizationId,
      name: form.name.trim(),
      description: form.description.trim() || undefined,
      unit: form.unit,
      requiresAttachment: form.requiresAttachment,
      isActive: form.isActive,
    };

    try {
      if (editingId == null) {
        await createLeaveType.mutateAsync(payload);
        toast.success("Leave type created.");
      } else {
        await updateLeaveType.mutateAsync({
          id: editingId,
          payload: {
            name: payload.name,
            description: payload.description,
            unit: payload.unit,
            requiresAttachment: payload.requiresAttachment,
            isActive: payload.isActive,
          },
        });
        toast.success("Leave type updated.");
      }
      startCreate();
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const remove = async () => {
    if (editingId == null) return;
    try {
      await deleteLeaveType.mutateAsync(editingId);
      toast.success("Leave type deleted.");
      startCreate();
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const isSaving = createLeaveType.isPending || updateLeaveType.isPending || deleteLeaveType.isPending;

  if (leaveTypesQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-foreground">Leave Setting</h1>
        <p className="mt-1 text-sm text-muted-foreground">Manage organization leave types and future leave settings.</p>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-medium">Configured leave types</h2>
          <Button type="button" variant="secondary" onClick={startCreate}>
            New leave type
          </Button>
        </div>
        <div className="mt-3 overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="text-left text-muted-foreground">
                <th className="pb-2 pr-3">Name</th>
                <th className="pb-2 pr-3">Unit</th>
                <th className="pb-2 pr-3">Attachment</th>
                <th className="pb-2 pr-3">Status</th>
                <th className="pb-2 pr-3">Action</th>
              </tr>
            </thead>
            <tbody>
              {(leaveTypesQuery.data ?? []).map((row) => (
                <tr key={row.id} className="border-t border-border">
                  <td className="py-2 pr-3">{row.name}</td>
                  <td className="py-2 pr-3">{unitLabel(row.unit)}</td>
                  <td className="py-2 pr-3">{row.requiresAttachment ? "Required" : "Optional"}</td>
                  <td className="py-2 pr-3">{row.isActive ? "Active" : "Inactive"}</td>
                  <td className="py-2 pr-3">
                    <Button type="button" variant="secondary" onClick={() => startEdit(row)}>
                      Edit
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <h2 className="text-lg font-medium">{editingId == null ? "Add leave type" : `Edit: ${selectedLeaveType?.name ?? "Leave type"}`}</h2>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <label className="block">
            <span className="text-xs font-medium text-muted-foreground">Name</span>
            <input
              value={form.name}
              onChange={(e) => setForm((prev) => ({ ...prev, name: e.target.value }))}
              className="mt-1 w-full rounded-lg border border-input px-3 py-2 text-sm dark:bg-card"
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium text-muted-foreground">Unit</span>
            <select
              value={form.unit}
              onChange={(e) => setForm((prev) => ({ ...prev, unit: Number(e.target.value) as LeaveUnitValue }))}
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
            >
              <option value={1}>Days</option>
              <option value={2}>Hours</option>
            </select>
          </label>
          <label className="block md:col-span-2">
            <span className="text-xs font-medium text-muted-foreground">Description</span>
            <input
              value={form.description}
              onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
              className="mt-1 w-full rounded-lg border border-input px-3 py-2 text-sm dark:bg-card"
            />
          </label>
          <label className="inline-flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={form.requiresAttachment}
              onChange={(e) => setForm((prev) => ({ ...prev, requiresAttachment: e.target.checked }))}
            />
            Requires attachment
          </label>
          <label className="inline-flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
            />
            Active
          </label>
        </div>

        <div className="mt-4 flex gap-2">
          <Button type="button" onClick={() => void submit()} disabled={isSaving}>
            {editingId == null ? "Create" : "Save"}
          </Button>
          {editingId != null ? (
            <Button type="button" variant="secondary" onClick={() => void remove()} disabled={isSaving}>
              Delete
            </Button>
          ) : null}
        </div>
      </div>
    </div>
  );
}

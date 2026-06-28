"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { PageHeader } from "@/shared/components/PageHeader";
import { SearchInput } from "@/shared/components/SearchInput";
import { SearchableSelect } from "@/shared/components/SearchableSelect";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { cn } from "@/shared/utils/cn";
import { toSelectOptions } from "@/shared/utils/to-select-options";
import {
  useCreateDepartment,
  useDeleteDepartment,
  useDepartments,
  useUpdateDepartment,
} from "../hooks";
import type { Department } from "../types/department.types";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

function formatDepartmentLabel(d: Department): string {
  return d.code ? `${d.name} (${d.code})` : d.name;
}

export function DepartmentsSection() {
  const { organizationId: currentOrgId } = useOrganizationContext();
  const { data, isLoading, isError, error } = useDepartments();
  const createMut = useCreateDepartment();
  const updateMut = useUpdateDepartment();
  const deleteMut = useDeleteDepartment();

  const [search, setSearch] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Department | null>(null);

  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [parentDepartmentId, setParentDepartmentId] = useState("");
  const [isActive, setIsActive] = useState(true);

  const filtered = useMemo(() => {
    const list = data ?? [];
    const q = search.trim().toLowerCase();
    if (!q) return list;
    return list.filter((d) => {
      const hay = `${d.name} ${d.code ?? ""} ${d.id}`.toLowerCase();
      return hay.includes(q);
    });
  }, [data, search]);

  const departmentNameById = useMemo(() => {
    const map = new Map<number, string>();
    for (const d of data ?? []) {
      map.set(d.id, d.name);
    }
    return map;
  }, [data]);

  const parentDepartmentOptions = useMemo(() => {
    const list = data ?? [];
    return toSelectOptions(
      list.filter(
        (d) => d.organizationId === currentOrgId && (editing == null || d.id !== editing.id),
      ),
      (d) => d.id,
      formatDepartmentLabel,
    );
  }, [data, currentOrgId, editing]);

  const openCreate = () => {
    setEditing(null);
    setName("");
    setCode("");
    setParentDepartmentId("");
    setIsActive(true);
    setFormOpen(true);
  };

  const openEdit = (d: Department) => {
    setEditing(d);
    setName(d.name);
    setCode(d.code ?? "");
    setParentDepartmentId(d.parentDepartmentId != null ? String(d.parentDepartmentId) : "");
    setIsActive(d.isActive);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
  };

  const parseOptionalInt = (s: string): number | null => {
    const t = s.trim();
    if (!t) return null;
    const n = Number(t);
    return Number.isFinite(n) ? n : null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (currentOrgId == null) {
      toast.error("Organization is not loaded.");
      return;
    }
    if (!name.trim()) {
      toast.error("Name is required.");
      return;
    }

    try {
      if (editing) {
        await updateMut.mutateAsync({
          id: editing.id,
          body: {
            name: name.trim(),
            code: code.trim() ? code.trim() : null,
            parentDepartmentId: parseOptionalInt(parentDepartmentId),
            isActive,
          },
        });
        toast.success("Department updated.");
      } else {
        await createMut.mutateAsync({
          organizationId: currentOrgId,
          name: name.trim(),
          code: code.trim() ? code.trim() : null,
          parentDepartmentId: parseOptionalInt(parentDepartmentId),
          isActive,
        });
        toast.success("Department created.");
      }
      closeForm();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const handleDelete = async (d: Department) => {
    if (!window.confirm(`Delete department “${d.name}”?`)) return;
    try {
      await deleteMut.mutateAsync(d.id);
      toast.success("Department deleted.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const busy = createMut.isPending || updateMut.isPending;

  if (currentOrgId == null) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Departments"
        description="Manage departments across your organization."
        actions={
          <Button type="button" onClick={openCreate}>
            Add department
          </Button>
        }
      />

      <SearchInput
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Name or code"
        aria-label="Search departments"
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
        <div className="overflow-hidden rounded-2xl border border-border bg-card shadow-soft">
          <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border text-left text-sm ">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-4 py-3 font-medium text-muted-foreground">#</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Name</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Code</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Parent</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Active</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filtered.map((row, index) => (
                <tr key={row.id} className="bg-card hover:bg-muted/40">
                  <td className="whitespace-nowrap px-4 py-3 font-mono text-muted-foreground">
                    {index + 1}
                  </td>
                  <td className="px-4 py-3 text-foreground">{row.name}</td>
                  <td className="px-4 py-3 text-muted-foreground">{row.code ?? "—"}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {row.parentDepartmentId != null ? (departmentNameById.get(row.parentDepartmentId) ?? "—") : "—"}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={cn(
                        "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                        row.isActive
                          ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200"
                          : "bg-muted text-muted-foreground",
                      )}
                    >
                      {row.isActive ? "Yes" : "No"}
                    </span>
                  </td>
                  <td className="whitespace-nowrap px-4 py-3">
                    <div className="flex gap-2">
                      <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => openEdit(row)}>
                        Edit
                      </Button>
                      <Button
                        type="button"
                        variant="danger"
                        className="!py-1 !text-xs"
                        onClick={() => void handleDelete(row)}
                        disabled={deleteMut.isPending}
                      >
                        Delete
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
          {filtered.length === 0 ? (
            <p className="border-t border-border p-6 text-center text-sm text-muted-foreground">No departments match the current filter.</p>
          ) : null}
        </div>
      )}

      <Modal
        open={formOpen}
        title={editing ? "Edit department" : "New department"}
        onClose={closeForm}
        className="max-w-lg"
      >
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Name</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={inputClass}
              required
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Code</label>
            <input
              type="text"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              className={inputClass}
              placeholder="Optional"
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Parent department (optional)
            </label>
            <SearchableSelect<number>
              options={parentDepartmentOptions}
              value={parseOptionalInt(parentDepartmentId)}
              onChange={(v) => setParentDepartmentId(v === null ? "" : String(v))}
              placeholder="Search parent department…"
              emptyLabel="No parent"
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-foreground">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="rounded border-input"
            />
            Active
          </label>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" onClick={closeForm}>
              Cancel
            </Button>
            <Button type="submit" disabled={busy}>
              {busy ? "Saving…" : editing ? "Save" : "Create"}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

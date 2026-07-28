"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { cn } from "@/shared/utils/cn";
import {
  useCreateJobPosition,
  useDeleteJobPosition,
  useJobPositions,
  usePositionRoles,
  useSetPositionRoles,
  useUpdateJobPosition,
} from "../hooks";
import type { JobPosition } from "../types/job-position.types";
import { fetchRoles } from "@/features/user-management/services/userManagementApi";
import { useQuery } from "@tanstack/react-query";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

export function JobPositionsSection() {
  const { organizationId: currentOrgId } = useOrganizationContext();

  const { data, isLoading, isError, error } = useJobPositions(currentOrgId);
  const createMut = useCreateJobPosition(currentOrgId);
  const updateMut = useUpdateJobPosition(currentOrgId);
  const deleteMut = useDeleteJobPosition(currentOrgId);
  const setPositionRolesMut = useSetPositionRoles(currentOrgId);
  const rolesCatalogQuery = useQuery({ queryKey: ["roles"], queryFn: fetchRoles });

  const [search, setSearch] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<JobPosition | null>(null);
  const [rolesPosition, setRolesPosition] = useState<JobPosition | null>(null);
  const [selectedRoleKeys, setSelectedRoleKeys] = useState<Set<string>>(new Set());
  const positionRolesQuery = usePositionRoles(rolesPosition?.id ?? null);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [code, setCode] = useState("");
  const [isActive, setIsActive] = useState(true);

  const filtered = useMemo(() => {
    const list = data ?? [];
    const q = search.trim().toLowerCase();
    if (!q) return list;
    return list.filter((p) => {
      const hay = `${p.title} ${p.code ?? ""} ${p.description ?? ""} ${p.id}`.toLowerCase();
      return hay.includes(q);
    });
  }, [data, search]);

  const openCreate = () => {
    setEditing(null);
    setTitle("");
    setDescription("");
    setCode("");
    setIsActive(true);
    setFormOpen(true);
  };

  const openEdit = (p: JobPosition) => {
    setEditing(p);
    setTitle(p.title);
    setDescription(p.description ?? "");
    setCode(p.code ?? "");
    setIsActive(p.isActive);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
  };

  const openRoles = (position: JobPosition) => {
    setRolesPosition(position);
    setSelectedRoleKeys(new Set());
  };

  const closeRoles = () => {
    setRolesPosition(null);
    setSelectedRoleKeys(new Set());
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (currentOrgId == null) {
      toast.error("Organization is not loaded.");
      return;
    }
    if (!title.trim()) {
      toast.error("Title is required.");
      return;
    }

    try {
      if (editing) {
        await updateMut.mutateAsync({
          id: editing.id,
          body: {
            title: title.trim(),
            description: description.trim() ? description.trim() : null,
            code: code.trim() ? code.trim() : null,
            isActive,
          },
        });
        toast.success("Position updated.");
      } else {
        await createMut.mutateAsync({
          organizationId: currentOrgId,
          title: title.trim(),
          description: description.trim() ? description.trim() : null,
          code: code.trim() ? code.trim() : null,
          isActive,
        });
        toast.success("Position created.");
      }
      closeForm();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const handleDelete = async (p: JobPosition) => {
    if (!window.confirm(`Delete position “${p.title}”?`)) return;
    try {
      await deleteMut.mutateAsync(p.id);
      toast.success("Position deleted.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const busy = createMut.isPending || updateMut.isPending;
  const assignedRoleKeys = useMemo(
    () => new Set((positionRolesQuery.data ?? []).map((role) => role.roleKey)),
    [positionRolesQuery.data],
  );
  const displayRoleKeys = selectedRoleKeys.size > 0 ? selectedRoleKeys : assignedRoleKeys;

  const toggleRoleSelection = (roleKey: string, checked: boolean) => {
    setSelectedRoleKeys((prev) => {
      const next = new Set(prev.size > 0 ? prev : assignedRoleKeys);
      if (checked) {
        next.add(roleKey);
      } else {
        next.delete(roleKey);
      }
      return next;
    });
  };

  const savePositionRoles = async () => {
    if (!rolesPosition) return;
    const roleKeys = Array.from(displayRoleKeys);
    const removed = Array.from(assignedRoleKeys).filter((roleKey) => !displayRoleKeys.has(roleKey));
    if (removed.length > 0 && !window.confirm("Remove selected role(s) from this position?")) {
      return;
    }

    try {
      await setPositionRolesMut.mutateAsync({ id: rolesPosition.id, roleKeys });
      toast.success("Position roles updated.");
      closeRoles();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  if (currentOrgId == null) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-foreground">Positions</h1>
          <p className="mt-1 text-sm text-muted-foreground">Manage available job positions.</p>
        </div>
        <Button type="button" onClick={openCreate}>
          Add position
        </Button>
      </div>

      <div className="max-w-md">
        <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
          Search
        </label>
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Title, code, description"
          className={inputClass}
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
        <div className="overflow-x-auto rounded-lg border border-border">
          <table className="min-w-full divide-y divide-border text-left text-sm ">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-4 py-3 font-medium text-muted-foreground">#</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Title</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Code</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Description</th>
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
                  <td className="px-4 py-3 text-foreground">{row.title}</td>
                  <td className="px-4 py-3 text-muted-foreground">{row.code ?? "—"}</td>
                  <td className="max-w-xs truncate px-4 py-3 text-muted-foreground" title={row.description ?? ""}>
                    {row.description ?? "—"}
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
                      <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => openRoles(row)}>
                        Roles
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
          {filtered.length === 0 ? (
            <p className="p-6 text-center text-sm text-muted-foreground">No positions yet (or no matches).</p>
          ) : null}
        </div>
      )}

      <Modal
        open={formOpen}
        title={editing ? "Edit position" : "New position"}
        onClose={closeForm}
        className="max-w-lg"
      >
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Title</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className={inputClass}
              required
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Description
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className={inputClass}
              rows={3}
              placeholder="Optional"
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

      <Modal
        open={rolesPosition != null}
        title={rolesPosition ? `Position roles: ${rolesPosition.title}` : "Position roles"}
        onClose={closeRoles}
        className="max-w-xl"
        footer={
          <>
            <Button type="button" variant="secondary" onClick={closeRoles}>
              Cancel
            </Button>
            <Button type="button" onClick={() => void savePositionRoles()} disabled={setPositionRolesMut.isPending}>
              {setPositionRolesMut.isPending ? "Saving…" : "Save roles"}
            </Button>
          </>
        }
      >
        {rolesCatalogQuery.isLoading || positionRolesQuery.isLoading ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : rolesCatalogQuery.isError || positionRolesQuery.isError ? (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
            {getErrorMessage(rolesCatalogQuery.error ?? positionRolesQuery.error)}
          </div>
        ) : (
          <div className="space-y-3">
            {(rolesCatalogQuery.data ?? []).map((role) => (
              <label key={role.normalizedName} className="flex items-start gap-3 rounded-lg border border-border px-3 py-2 dark:border-border">
                <input
                  type="checkbox"
                  className="mt-0.5 size-4 rounded border-input"
                  checked={displayRoleKeys.has(role.normalizedName)}
                  onChange={(e) => toggleRoleSelection(role.normalizedName, e.target.checked)}
                />
                <span className="text-sm">
                  <span className="font-medium text-foreground">{role.name}</span>
                  {role.description ? (
                    <span className="mt-0.5 block text-xs text-muted-foreground">{role.description}</span>
                  ) : null}
                </span>
              </label>
            ))}
          </div>
        )}
      </Modal>
    </div>
  );
}

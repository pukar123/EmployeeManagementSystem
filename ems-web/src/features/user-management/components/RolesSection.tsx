"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { cn } from "@/shared/utils/cn";
import type { RoleDto } from "../types";
import { createRole, deleteRole, fetchRoles, updateRole } from "../services/userManagementApi";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

export function RolesSection() {
  const queryClient = useQueryClient();
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["roles"],
    queryFn: fetchRoles,
  });

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<RoleDto | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  const createMut = useMutation({
    mutationFn: createRole,
    onSuccess: async () => {
      toast.success("Role created.");
      await queryClient.invalidateQueries({ queryKey: ["roles"] });
      closeForm();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const updateMut = useMutation({
    mutationFn: ({ id, body }: { id: number; body: Parameters<typeof updateRole>[1] }) => updateRole(id, body),
    onSuccess: async () => {
      toast.success("Role updated.");
      await queryClient.invalidateQueries({ queryKey: ["roles"] });
      closeForm();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const deleteMut = useMutation({
    mutationFn: deleteRole,
    onSuccess: async () => {
      toast.success("Role deleted.");
      await queryClient.invalidateQueries({ queryKey: ["roles"] });
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const openCreate = () => {
    setEditing(null);
    setName("");
    setDescription("");
    setFormOpen(true);
  };

  const openEdit = (r: RoleDto) => {
    setEditing(r);
    setName(r.name);
    setDescription(r.description ?? "");
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const n = name.trim();
    if (!n) {
      toast.error("Name is required.");
      return;
    }
    try {
      if (editing) {
        await updateMut.mutateAsync({
          id: editing.id,
          body: { name: n, description: description.trim() ? description.trim() : null },
        });
      } else {
        await createMut.mutateAsync({ name: n, description: description.trim() ? description.trim() : null });
      }
    } catch {
      /* toast in mutation */
    }
  };

  const handleDelete = async (r: RoleDto) => {
    if (r.isSystem) {
      toast.error("System roles cannot be deleted.");
      return;
    }
    if (!window.confirm(`Delete role “${r.name}”?`)) return;
    try {
      await deleteMut.mutateAsync(r.id);
    } catch {
      /* toast in mutation */
    }
  };

  const busy = createMut.isPending || updateMut.isPending;

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400" role="alert">
        {getErrorMessage(error)}
      </p>
    );
  }

  const roles = data ?? [];

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">Roles</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Create and edit roles here. System roles cannot be changed or removed.
          </p>
        </div>
        <Button type="button" onClick={openCreate}>
          Add role
        </Button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-border">
        <table className="min-w-full divide-y divide-border text-left text-sm ">
          <thead className="bg-muted/50">
            <tr>
              <th className="px-4 py-3 font-medium text-muted-foreground">Name</th>
              <th className="px-4 py-3 font-medium text-muted-foreground">Description</th>
              <th className="px-4 py-3 font-medium text-muted-foreground">System</th>
              <th className="px-4 py-3 font-medium text-muted-foreground">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {roles.map((r) => (
              <tr key={r.id} className="bg-card hover:bg-muted/40">
                <td className="px-4 py-3 text-foreground">{r.name}</td>
                <td className="px-4 py-3 text-muted-foreground">{r.description ?? "—"}</td>
                <td className="px-4 py-3">
                  <span
                    className={cn(
                      "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                      r.isSystem
                        ? "bg-amber-100 text-amber-900 dark:bg-amber-900/40 dark:text-amber-200"
                        : "bg-muted text-muted-foreground",
                    )}
                  >
                    {r.isSystem ? "Yes" : "No"}
                  </span>
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="secondary"
                      className="!py-1 !text-xs"
                      onClick={() => openEdit(r)}
                      disabled={r.isSystem}
                    >
                      Edit
                    </Button>
                    <Button
                      type="button"
                      variant="danger"
                      className="!py-1 !text-xs"
                      onClick={() => void handleDelete(r)}
                      disabled={r.isSystem || deleteMut.isPending}
                    >
                      Delete
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {roles.length === 0 ? (
          <p className="p-6 text-center text-sm text-muted-foreground">No roles yet.</p>
        ) : null}
      </div>

      <Modal
        open={formOpen}
        title={editing ? "Edit role" : "New role"}
        onClose={closeForm}
        className="max-w-lg"
      >
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Name
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={inputClass}
              required
              disabled={editing?.isSystem === true}
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Description
            </label>
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className={inputClass}
              placeholder="Optional"
              disabled={editing?.isSystem === true}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" onClick={closeForm}>
              Cancel
            </Button>
            <Button type="submit" disabled={busy || editing?.isSystem === true}>
              {busy ? "Saving…" : editing ? "Save" : "Create"}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

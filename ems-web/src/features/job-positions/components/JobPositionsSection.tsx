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
  useUpdateJobPosition,
} from "../hooks";
import type { JobPosition } from "../types/job-position.types";

const inputClass =
  "mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100";

export function JobPositionsSection() {
  const { organizationId: currentOrgId } = useOrganizationContext();

  const { data, isLoading, isError, error } = useJobPositions(currentOrgId);
  const createMut = useCreateJobPosition(currentOrgId);
  const updateMut = useUpdateJobPosition(currentOrgId);
  const deleteMut = useDeleteJobPosition(currentOrgId);

  const [search, setSearch] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<JobPosition | null>(null);

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
          <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Positions</h1>
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Manage available job positions.</p>
        </div>
        <Button type="button" onClick={openCreate}>
          Add position
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
        <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-700">
          <table className="min-w-full divide-y divide-zinc-200 text-left text-sm dark:divide-zinc-700">
            <thead className="bg-zinc-50 dark:bg-zinc-900/50">
              <tr>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">#</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Title</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Code</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Description</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Active</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-zinc-200 dark:divide-zinc-700">
              {filtered.map((row, index) => (
                <tr key={row.id} className="bg-white hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900">
                  <td className="whitespace-nowrap px-4 py-3 font-mono text-zinc-600 dark:text-zinc-400">
                    {index + 1}
                  </td>
                  <td className="px-4 py-3 text-zinc-900 dark:text-zinc-100">{row.title}</td>
                  <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{row.code ?? "—"}</td>
                  <td className="max-w-xs truncate px-4 py-3 text-zinc-600 dark:text-zinc-400" title={row.description ?? ""}>
                    {row.description ?? "—"}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={cn(
                        "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                        row.isActive
                          ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200"
                          : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300",
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
          {filtered.length === 0 ? (
            <p className="p-6 text-center text-sm text-zinc-500">No positions yet (or no matches).</p>
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
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Title</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className={inputClass}
              required
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
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
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Code</label>
            <input
              type="text"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              className={inputClass}
              placeholder="Optional"
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-zinc-800 dark:text-zinc-200">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="rounded border-zinc-300 dark:border-zinc-600"
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

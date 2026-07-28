"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { cn } from "@/shared/utils/cn";
import { useCreateSite, useDeleteSite, useSites, useUpdateSite } from "../hooks";
import type { Site } from "../types/site.types";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

export function SitesSection() {
  const { data, isLoading, isError, error } = useSites();
  const createMut = useCreateSite();
  const updateMut = useUpdateSite();
  const deleteMut = useDeleteSite();

  const [search, setSearch] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Site | null>(null);

  const [siteName, setSiteName] = useState("");
  const [siteDescription, setSiteDescription] = useState("");
  const [siteLocation, setSiteLocation] = useState("");
  const [isActive, setIsActive] = useState(true);

  const filtered = useMemo(() => {
    const list = data ?? [];
    const q = search.trim().toLowerCase();
    if (!q) return list;
    return list.filter((s) => {
      const hay = `${s.siteName} ${s.siteLocation} ${s.siteDescription ?? ""} ${s.siteId}`.toLowerCase();
      return hay.includes(q);
    });
  }, [data, search]);

  const openCreate = () => {
    setEditing(null);
    setSiteName("");
    setSiteDescription("");
    setSiteLocation("");
    setIsActive(true);
    setFormOpen(true);
  };

  const openEdit = (s: Site) => {
    setEditing(s);
    setSiteName(s.siteName);
    setSiteDescription(s.siteDescription ?? "");
    setSiteLocation(s.siteLocation);
    setIsActive(s.isActive);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!siteName.trim()) {
      toast.error("Site name is required.");
      return;
    }
    if (!siteLocation.trim()) {
      toast.error("Location is required.");
      return;
    }

    try {
      const desc = siteDescription.trim() ? siteDescription.trim() : null;
      if (editing) {
        await updateMut.mutateAsync({
          id: editing.siteId,
          body: {
            siteName: siteName.trim(),
            siteDescription: desc,
            siteLocation: siteLocation.trim(),
            isActive,
          },
        });
        toast.success("Site updated.");
      } else {
        await createMut.mutateAsync({
          siteName: siteName.trim(),
          siteDescription: desc,
          siteLocation: siteLocation.trim(),
          isActive,
        });
        toast.success("Site created.");
      }
      closeForm();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const handleDelete = async (s: Site) => {
    if (!window.confirm(`Delete site “${s.siteName}”?`)) return;
    try {
      await deleteMut.mutateAsync(s.siteId);
      toast.success("Site deleted.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const busy = createMut.isPending || updateMut.isPending;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-foreground">Sites</h1>
          <p className="mt-1 text-sm text-muted-foreground">Manage office and work locations.</p>
        </div>
        <Button type="button" onClick={openCreate}>
          Add site
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
          placeholder="Name, location, or id"
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
                <th className="px-4 py-3 font-medium text-muted-foreground">Name</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Location</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Description</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Active</th>
                <th className="px-4 py-3 font-medium text-muted-foreground">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filtered.map((row, index) => (
                <tr key={row.siteId} className="bg-card hover:bg-muted/40">
                  <td className="whitespace-nowrap px-4 py-3 font-mono text-muted-foreground">
                    {index + 1}
                  </td>
                  <td className="px-4 py-3 text-foreground">{row.siteName}</td>
                  <td className="max-w-[12rem] px-4 py-3 text-muted-foreground">{row.siteLocation}</td>
                  <td className="max-w-xs px-4 py-3 text-muted-foreground">
                    {row.siteDescription ? (
                      <span className="line-clamp-2" title={row.siteDescription}>
                        {row.siteDescription}
                      </span>
                    ) : (
                      "—"
                    )}
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
          {filtered.length === 0 ? (
            <p className="p-6 text-center text-sm text-muted-foreground">No sites match the current filter.</p>
          ) : null}
        </div>
      )}

      <Modal open={formOpen} title={editing ? "Edit site" : "New site"} onClose={closeForm} className="max-w-lg">
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Site name
            </label>
            <input
              type="text"
              value={siteName}
              onChange={(e) => setSiteName(e.target.value)}
              className={inputClass}
              required
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Location
            </label>
            <input
              type="text"
              value={siteLocation}
              onChange={(e) => setSiteLocation(e.target.value)}
              className={inputClass}
              required
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Description
            </label>
            <textarea
              value={siteDescription}
              onChange={(e) => setSiteDescription(e.target.value)}
              className={`${inputClass} min-h-[88px] resize-y`}
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
    </div>
  );
}

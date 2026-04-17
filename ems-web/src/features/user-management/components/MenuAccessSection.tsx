"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import type { MenuFlatDto } from "../types";
import {
  fetchMenusFlat,
  fetchRoleMenuAccess,
  setRoleMenuAccess,
} from "../services/roleMenuAccessApi";
import { fetchRoles } from "../services/userManagementApi";

function collectDescendantIds(flat: MenuFlatDto[], rootId: number): number[] {
  const direct = flat.filter((m) => m.parentMenuId === rootId).map((m) => m.id);
  const nested = direct.flatMap((id) => collectDescendantIds(flat, id));
  return [...direct, ...nested];
}

function collectAncestorIds(flat: MenuFlatDto[], menuId: number): number[] {
  const byId = new Map(flat.map((m) => [m.id, m]));
  const out: number[] = [];
  let cur = byId.get(menuId);
  while (cur?.parentMenuId != null) {
    out.push(cur.parentMenuId);
    cur = byId.get(cur.parentMenuId);
  }
  return out;
}

/** Ensure parents are included so navigation tree can resolve (matches server-side parent expansion). */
function expandMenuIdsWithParents(flat: MenuFlatDto[], selected: ReadonlySet<number>): number[] {
  const next = new Set(selected);
  for (const id of selected) {
    for (const p of collectAncestorIds(flat, id)) {
      next.add(p);
    }
  }
  return [...next].sort((a, b) => a - b);
}

export function MenuAccessSection() {
  const queryClient = useQueryClient();
  const { data: roles, isLoading: rolesLoading, isError: rolesError, error: rolesErr } = useQuery({
    queryKey: ["roles"],
    queryFn: fetchRoles,
  });

  const { data: menus, isLoading: menusLoading } = useQuery({
    queryKey: ["menus-flat"],
    queryFn: fetchMenusFlat,
  });

  const [roleKey, setRoleKey] = useState<string | null>(null);

  const {
    data: access,
    isLoading: accessLoading,
    isFetching: accessFetching,
  } = useQuery({
    queryKey: ["role-menu-access", roleKey],
    queryFn: () => fetchRoleMenuAccess(roleKey!),
    enabled: Boolean(roleKey),
  });

  const [selected, setSelected] = useState<Set<number>>(new Set());

  useEffect(() => {
    if (!access?.menuIds) {
      setSelected(new Set());
      return;
    }
    setSelected(new Set(access.menuIds));
  }, [access?.menuIds, roleKey]);

  const flatSorted = useMemo(() => {
    if (!menus?.length) return [];
    return [...menus].sort((a, b) => a.sortOrder - b.sortOrder || a.label.localeCompare(b.label));
  }, [menus]);

  const byId = useMemo(() => new Map(flatSorted.map((m) => [m.id, m])), [flatSorted]);

  const depth = useCallback(
    (m: MenuFlatDto) => {
      let d = 0;
      let cur: MenuFlatDto | undefined = m;
      while (cur?.parentMenuId != null) {
        d++;
        cur = byId.get(cur.parentMenuId);
      }
      return d;
    },
    [byId],
  );

  const toggle = useCallback(
    (menuId: number, checked: boolean) => {
      if (!menus?.length) return;
      setSelected((prev) => {
        const next = new Set(prev);
        if (checked) {
          next.add(menuId);
          for (const a of collectAncestorIds(menus, menuId)) {
            next.add(a);
          }
        } else {
          next.delete(menuId);
          for (const d of collectDescendantIds(menus, menuId)) {
            next.delete(d);
          }
        }
        return next;
      });
    },
    [menus],
  );

  const saveMut = useMutation({
    mutationFn: async () => {
      if (!roleKey || !menus?.length) return;
      const menuIds = expandMenuIdsWithParents(menus, selected);
      await setRoleMenuAccess(roleKey, menuIds);
    },
    onSuccess: async () => {
      toast.success("Menu access saved.");
      await queryClient.invalidateQueries({ queryKey: ["role-menu-access", roleKey] });
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const busy = rolesLoading || menusLoading;
  if (busy) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (rolesError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400" role="alert">
        {getErrorMessage(rolesErr)}
      </p>
    );
  }

  const roleList = roles ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">Menu access</h1>
        <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
          Choose a role and select which menus that role may access. Parent menus are included automatically when a
          child is selected.
        </p>
      </div>

      <div className="max-w-xl space-y-2">
        <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400" htmlFor="role">
          Role
        </label>
        <select
          id="role"
          className="mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100"
          value={roleKey ?? ""}
          onChange={(e) => setRoleKey(e.target.value || null)}
        >
          <option value="">Select a role…</option>
          {roleList.map((r) => (
            <option key={r.id} value={r.normalizedName}>
              {r.name} ({r.normalizedName})
            </option>
          ))}
        </select>
      </div>

      {!roleKey ? (
        <p className="text-sm text-zinc-500">Select a role to view and edit its menus.</p>
      ) : accessLoading || accessFetching ? (
        <div className="flex justify-center py-12">
          <Spinner />
        </div>
      ) : (
        <>
          <div className="overflow-x-auto rounded-xl border border-zinc-200 dark:border-zinc-700">
            <table className="min-w-full divide-y divide-zinc-200 text-left text-sm dark:divide-zinc-700">
              <thead className="bg-zinc-50 dark:bg-zinc-900/50">
                <tr>
                  <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Allow</th>
                  <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Menu</th>
                  <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Route</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-200 dark:divide-zinc-700">
                {flatSorted.map((m) => {
                  const d = depth(m);
                  return (
                    <tr key={m.id} className="bg-white dark:bg-zinc-950">
                      <td className="px-4 py-2">
                        <input
                          type="checkbox"
                          className="h-4 w-4 rounded border-zinc-300 text-zinc-900 focus:ring-zinc-500"
                          checked={selected.has(m.id)}
                          onChange={(e) => toggle(m.id, e.target.checked)}
                        />
                      </td>
                      <td className="px-4 py-2 text-zinc-900 dark:text-zinc-100" style={{ paddingLeft: `${1 + d * 1}rem` }}>
                        {m.label}
                      </td>
                      <td className="px-4 py-2 text-zinc-600 dark:text-zinc-400">{m.routePath}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="flex justify-end gap-2">
            <Button
              type="button"
              onClick={() => saveMut.mutate()}
              disabled={saveMut.isPending || !roleKey}
            >
              {saveMut.isPending ? "Saving…" : "Save"}
            </Button>
          </div>
        </>
      )}
    </div>
  );
}

"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import type { MenuFlatDto, RolePermissionItemDto } from "../types";
import {
  fetchMenusFlat,
  fetchRolePermissions,
  fetchRoles,
  saveRolePermissions,
} from "../services/userManagementApi";

function sortMenusFlat(menus: MenuFlatDto[]): MenuFlatDto[] {
  return [...menus].sort((a, b) => a.sortOrder - b.sortOrder || a.label.localeCompare(b.label));
}

function buildPermissionMap(items: RolePermissionItemDto[]): Map<number, boolean> {
  const map = new Map<number, boolean>();
  for (const p of items) {
    map.set(p.menuId, p.allowed);
  }
  return map;
}

export function PermissionsSection() {
  const queryClient = useQueryClient();
  const rolesQuery = useQuery({ queryKey: ["roles"], queryFn: fetchRoles });
  const menusQuery = useQuery({ queryKey: ["menus-flat"], queryFn: fetchMenusFlat });

  const [selectedRoleId, setSelectedRoleId] = useState<number | null>(null);
  const [allowedByMenuId, setAllowedByMenuId] = useState<Map<number, boolean>>(new Map());

  const permsQuery = useQuery({
    queryKey: ["role-permissions", selectedRoleId],
    queryFn: () => fetchRolePermissions(selectedRoleId!),
    enabled: selectedRoleId != null,
  });

  useEffect(() => {
    if (permsQuery.data) {
      setAllowedByMenuId(buildPermissionMap(permsQuery.data));
    }
  }, [permsQuery.data]);

  const flatMenus = useMemo(() => sortMenusFlat(menusQuery.data ?? []), [menusQuery.data]);

  const saveMut = useMutation({
    mutationFn: async () => {
      if (selectedRoleId == null) return;
      const permissions: RolePermissionItemDto[] = [];
      for (const menu of flatMenus) {
        permissions.push({
          menuId: menu.id,
          allowed: allowedByMenuId.get(menu.id) ?? false,
        });
      }
      await saveRolePermissions(selectedRoleId, permissions);
    },
    onSuccess: async () => {
      toast.success("Menu access saved.");
      await queryClient.invalidateQueries({ queryKey: ["role-permissions", selectedRoleId] });
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const toggle = useCallback((menuId: number, value: boolean) => {
    setAllowedByMenuId((prev) => {
      const next = new Map(prev);
      next.set(menuId, value);
      return next;
    });
  }, []);

  if (rolesQuery.isLoading || menusQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (rolesQuery.isError || menusQuery.isError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">Failed to load roles or menus.</p>
    );
  }

  const roles = rolesQuery.data ?? [];

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">
        Menu access
      </h1>
      <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
        Choose a role and toggle which EMS menu entries that role may access.
      </p>

      <div className="mt-6 flex flex-wrap items-end gap-4">
        <label className="block min-w-[200px]">
          <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Role</span>
          <select
            className="mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
            value={selectedRoleId ?? ""}
            onChange={(e) => {
              const v = e.target.value;
              setSelectedRoleId(v === "" ? null : Number(v));
            }}
          >
            <option value="">Select role…</option>
            {roles.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
                {r.isSystem ? " (system)" : ""}
              </option>
            ))}
          </select>
        </label>
        <Button
          type="button"
          variant="primary"
          disabled={selectedRoleId == null || saveMut.isPending || permsQuery.isLoading}
          onClick={() => saveMut.mutate()}
        >
          {saveMut.isPending ? "Saving…" : "Save"}
        </Button>
      </div>

      {selectedRoleId != null && permsQuery.isLoading ? (
        <div className="mt-8 flex justify-center">
          <Spinner />
        </div>
      ) : selectedRoleId != null ? (
        <ul className="mt-8 space-y-2">
          {flatMenus.map((menu) => (
            <li
              key={menu.id}
              className="flex items-center gap-3 rounded-lg border border-zinc-200 px-3 py-2 dark:border-zinc-700"
              style={{ marginLeft: menu.parentMenuId ? 16 : 0 }}
            >
              <input
                type="checkbox"
                className="size-4 rounded border-zinc-300"
                checked={allowedByMenuId.get(menu.id) ?? false}
                onChange={(e) => toggle(menu.id, e.target.checked)}
              />
              <span className="text-sm text-zinc-800 dark:text-zinc-200">{menu.label}</span>
              <span className="text-xs text-zinc-500">{menu.routePath}</span>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

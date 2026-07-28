"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useMemo, useState } from "react";
import { ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
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

function collectNodeAndDescendantIds(flat: MenuFlatDto[], rootId: number): number[] {
  return [rootId, ...collectDescendantIds(flat, rootId)];
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

  const flatSorted = useMemo(() => {
    if (!menus?.length) return [];
    return [...menus].sort((a, b) => a.sortOrder - b.sortOrder || a.label.localeCompare(b.label));
  }, [menus]);

  const saveMut = useMutation({
    mutationFn: async (menuIds: number[]) => {
      if (!roleKey) return;
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
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Menu access</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Choose a role and select which menus that role may access. Parent menus are included automatically when a
          child is selected.
        </p>
      </div>

      <div className="max-w-xl space-y-2">
        <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground" htmlFor="role">
          Role
        </label>
        <select
          id="role"
          className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground"
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
        <p className="text-sm text-muted-foreground">Select a role to view and edit its menus.</p>
      ) : accessLoading || accessFetching ? (
        <div className="flex justify-center py-12">
          <Spinner />
        </div>
      ) : (
        <>
          <MenuAccessTreeEditor
            key={`${roleKey}-${(access?.menuIds ?? []).join("-")}`}
            menus={flatSorted}
            initialSelectedIds={access?.menuIds ?? []}
            saving={saveMut.isPending}
            onSave={(selected) => {
              if (!roleKey || !menus?.length) return;
              saveMut.mutate(expandMenuIdsWithParents(menus, selected));
            }}
          />
        </>
      )}
    </div>
  );
}

function MenuAccessTreeEditor({
  menus,
  initialSelectedIds,
  saving,
  onSave,
}: {
  menus: MenuFlatDto[];
  initialSelectedIds: number[];
  saving: boolean;
  onSave: (selected: ReadonlySet<number>) => void;
}) {
  const [selected, setSelected] = useState<Set<number>>(new Set(initialSelectedIds));
  const roots = useMemo(() => menus.filter((m) => m.parentMenuId == null), [menus]);
  const childrenByParent = useMemo(() => {
    const map = new Map<number, MenuFlatDto[]>();
    for (const menu of menus) {
      if (menu.parentMenuId == null) continue;
      const list = map.get(menu.parentMenuId) ?? [];
      list.push(menu);
      map.set(menu.parentMenuId, list);
    }
    return map;
  }, [menus]);
  const [expanded, setExpanded] = useState<Set<number>>(new Set(roots.map((r) => r.id)));

  const toggle = useCallback(
    (menuId: number, checked: boolean) => {
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

  const toggleBranch = useCallback(
    (menuId: number, checked: boolean) => {
      const scope = new Set(collectNodeAndDescendantIds(menus, menuId));
      setSelected((prev) => {
        const next = new Set(prev);
        if (checked) {
          for (const id of scope) next.add(id);
          for (const id of scope) {
            for (const a of collectAncestorIds(menus, id)) next.add(a);
          }
        } else {
          for (const id of scope) next.delete(id);
        }
        return next;
      });
    },
    [menus],
  );

  const setExpandedForNode = useCallback((menuId: number, open: boolean) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (open) next.add(menuId);
      else next.delete(menuId);
      return next;
    });
  }, []);

  const getNodeState = useCallback(
    (menuId: number) => {
      const ids = collectNodeAndDescendantIds(menus, menuId);
      const checkedCount = ids.filter((id) => selected.has(id)).length;
      return {
        checked: checkedCount === ids.length && ids.length > 0,
        indeterminate: checkedCount > 0 && checkedCount < ids.length,
      };
    },
    [menus, selected],
  );

  return (
    <>
      <div className="rounded-xl border border-border bg-card p-2 dark:bg-card">
        {roots.map((root) => (
          <MenuPermissionNode
            key={root.id}
            node={root}
            level={0}
            childrenByParent={childrenByParent}
            expanded={expanded}
            setExpandedForNode={setExpandedForNode}
            onToggle={toggle}
            onToggleBranch={toggleBranch}
            getNodeState={getNodeState}
          />
        ))}
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" onClick={() => onSave(selected)} disabled={saving}>
          {saving ? "Saving…" : "Save"}
        </Button>
      </div>
    </>
  );
}

function MenuPermissionNode({
  node,
  level,
  childrenByParent,
  expanded,
  setExpandedForNode,
  onToggle,
  onToggleBranch,
  getNodeState,
}: {
  node: MenuFlatDto;
  level: number;
  childrenByParent: Map<number, MenuFlatDto[]>;
  expanded: Set<number>;
  setExpandedForNode: (menuId: number, open: boolean) => void;
  onToggle: (menuId: number, checked: boolean) => void;
  onToggleBranch: (menuId: number, checked: boolean) => void;
  getNodeState: (menuId: number) => { checked: boolean; indeterminate: boolean };
}) {
  const children = childrenByParent.get(node.id) ?? [];
  const hasChildren = children.length > 0;
  const open = expanded.has(node.id);
  const state = getNodeState(node.id);

  return (
    <div className="rounded-md">
      <div
        className="flex flex-wrap items-center gap-2 rounded-md px-2 py-2 hover:bg-muted/40/60"
        style={{ paddingLeft: `${0.5 + level * 1.1}rem` }}
      >
        {hasChildren ? (
          <button
            type="button"
            onClick={() => setExpandedForNode(node.id, !open)}
            className="inline-flex h-6 w-6 items-center justify-center rounded hover:bg-muted/70"
            aria-label={open ? "Collapse branch" : "Expand branch"}
          >
            <ChevronRight className={cn("size-4 transition-transform", open && "rotate-90")} />
          </button>
        ) : (
          <span className="inline-block h-6 w-6" />
        )}
        <TriStateCheckbox
          checked={state.checked}
          indeterminate={state.indeterminate}
          onChange={(checked) => onToggle(node.id, checked)}
        />
        <span className="min-w-[14rem] flex-1 text-sm text-foreground">{node.label}</span>
        <span className="min-w-[12rem] flex-1 text-xs text-muted-foreground">{node.routePath}</span>
        {hasChildren ? (
          <div className="ml-auto flex items-center gap-1">
            <Button type="button" variant="secondary" className="h-7 px-2 text-xs" onClick={() => onToggleBranch(node.id, true)}>
              Select all
            </Button>
            <Button type="button" variant="secondary" className="h-7 px-2 text-xs" onClick={() => onToggleBranch(node.id, false)}>
              Clear all
            </Button>
          </div>
        ) : null}
      </div>
      {hasChildren && open ? (
        <div className="pb-1">
          {children.map((child) => (
            <MenuPermissionNode
              key={child.id}
              node={child}
              level={level + 1}
              childrenByParent={childrenByParent}
              expanded={expanded}
              setExpandedForNode={setExpandedForNode}
              onToggle={onToggle}
              onToggleBranch={onToggleBranch}
              getNodeState={getNodeState}
            />
          ))}
        </div>
      ) : null}
    </div>
  );
}

function TriStateCheckbox({
  checked,
  indeterminate,
  onChange,
}: {
  checked: boolean;
  indeterminate: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <input
      ref={(el) => {
        if (el) {
          el.indeterminate = indeterminate;
        }
      }}
      type="checkbox"
      className="h-4 w-4 rounded border-input text-foreground focus:ring-primary/20"
      checked={checked}
      onChange={(e) => onChange(e.target.checked)}
      aria-checked={indeterminate ? "mixed" : checked}
    />
  );
}

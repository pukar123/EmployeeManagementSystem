"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import {
  EMPLOYEE_CAPABILITY_KEYS,
  EMPLOYEE_CAPABILITY_LABELS,
  expandCapabilityKeys,
  fetchRoleCapabilities,
  setRoleCapabilities,
  type EmployeeCapabilityKey,
} from "@/features/employees/services/employeeAccessApi";
import { fetchRoles } from "../services/userManagementApi";

export function EmployeeCapabilitiesSection() {
  const queryClient = useQueryClient();
  const { data: roles, isLoading: rolesLoading, isError: rolesError, error: rolesErr } = useQuery({
    queryKey: ["roles"],
    queryFn: fetchRoles,
  });

  const [roleKey, setRoleKey] = useState<string | null>(null);
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const {
    data: access,
    isLoading: accessLoading,
    isFetching: accessFetching,
  } = useQuery({
    queryKey: ["role-capabilities", roleKey],
    queryFn: () => fetchRoleCapabilities(roleKey!),
    enabled: Boolean(roleKey),
  });

  useEffect(() => {
    if (access?.capabilityKeys) {
      setSelected(new Set(access.capabilityKeys));
    }
  }, [access?.capabilityKeys]);

  const saveMut = useMutation({
    mutationFn: async (capabilityKeys: string[]) => {
      if (!roleKey) return;
      await setRoleCapabilities(roleKey, capabilityKeys);
    },
    onSuccess: async () => {
      toast.success("Employee capabilities saved.");
      await queryClient.invalidateQueries({ queryKey: ["role-capabilities", roleKey] });
      await queryClient.invalidateQueries({ queryKey: ["employee-access", "me"] });
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const toggle = useCallback((key: EmployeeCapabilityKey) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
        if (key === "employees.view") {
          next.delete("employees.manage");
          next.delete("employees.access");
          next.delete("employees.export");
        }
      } else {
        next.add(key);
        if (key === "employees.manage" || key === "employees.access" || key === "employees.export") {
          next.add("employees.view");
        }
      }
      return next;
    });
  }, []);

  if (rolesLoading) {
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
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Employee capabilities</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Assign what each role may do in the Employees module. The Employees menu controls navigation only; these
          capabilities control data access and actions.
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        {roleList.map((role) => (
          <button
            key={role.id}
            type="button"
            onClick={() => setRoleKey(role.normalizedName)}
            className={`rounded-lg border px-3 py-1.5 text-sm ${
              roleKey === role.normalizedName
                ? "border-primary bg-primary/10 font-medium text-primary"
                : "border-border bg-card text-foreground hover:border-input"
            }`}
          >
            {role.name}
          </button>
        ))}
      </div>

      {!roleKey ? (
        <p className="text-sm text-muted-foreground">Select a role to edit employee capabilities.</p>
      ) : accessLoading || accessFetching ? (
        <Spinner />
      ) : (
        <div className="space-y-4 rounded-xl border border-border bg-card p-5">
          <p className="text-sm font-medium text-foreground">Role: {roleKey}</p>
          <ul className="space-y-3">
            {EMPLOYEE_CAPABILITY_KEYS.map((key) => (
              <li key={key}>
                <label className="flex cursor-pointer items-start gap-3">
                  <input
                    type="checkbox"
                    checked={selected.has(key)}
                    onChange={() => toggle(key)}
                    className="mt-1"
                  />
                  <span>
                    <span className="block text-sm font-medium text-foreground">
                      {EMPLOYEE_CAPABILITY_LABELS[key]}
                    </span>
                    <span className="block text-xs text-muted-foreground">{key}</span>
                  </span>
                </label>
              </li>
            ))}
          </ul>
          <p className="text-xs text-muted-foreground">
            Manage, account access, and export automatically include view.
          </p>
          <Button
            type="button"
            disabled={saveMut.isPending}
            onClick={() => saveMut.mutate(expandCapabilityKeys(selected))}
          >
            Save capabilities
          </Button>
        </div>
      )}
    </div>
  );
}

"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import {
  fetchRoleCapabilities,
  setRoleCapabilities,
} from "@/features/employees/services/employeeAccessApi";
import { fetchRoles } from "../services/userManagementApi";
import { CapabilityEditor } from "./CapabilityEditor";
import { useState } from "react";

export function EmployeeCapabilitiesSection() {
  const queryClient = useQueryClient();
  const { data: roles, isLoading: rolesLoading, isError: rolesError, error: rolesErr } = useQuery({
    queryKey: ["roles"],
    queryFn: fetchRoles,
  });

  const [roleKey, setRoleKey] = useState<string | null>(null);

  const {
    data: access,
    isLoading: accessLoading,
    isFetching: accessFetching,
  } = useQuery({
    queryKey: ["role-capabilities", roleKey],
    queryFn: () => fetchRoleCapabilities(roleKey!),
    enabled: Boolean(roleKey),
  });

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
        <CapabilityEditor
          key={`${roleKey}:${(access?.capabilityKeys ?? []).join(",")}`}
          roleKey={roleKey}
          initialKeys={access?.capabilityKeys ?? []}
          isSaving={saveMut.isPending}
          onSave={(keys) => saveMut.mutate(keys)}
        />
      )}
    </div>
  );
}

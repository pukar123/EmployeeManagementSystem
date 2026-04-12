"use client";

import { useQuery } from "@tanstack/react-query";
import { Spinner } from "@/shared/components/Spinner";
import { fetchRoles } from "../services/userManagementApi";

export function RolesSection() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["roles"],
    queryFn: fetchRoles,
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {error instanceof Error ? error.message : "Failed to load roles."}
      </p>
    );
  }

  const roles = data ?? [];

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">Roles</h1>
      <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
        Application roles (managed in user management). System roles cannot be edited here.
      </p>
      <div className="mt-8 overflow-x-auto rounded-xl border border-zinc-200 dark:border-zinc-700">
        <table className="min-w-full text-left text-sm">
          <thead className="border-b border-zinc-200 bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-900/50">
            <tr>
              <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Name</th>
              <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Description</th>
              <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">System</th>
            </tr>
          </thead>
          <tbody>
            {roles.map((r) => (
              <tr
                key={r.id}
                className="border-b border-zinc-100 last:border-0 dark:border-zinc-800"
              >
                <td className="px-4 py-3 text-zinc-900 dark:text-zinc-100">{r.name}</td>
                <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{r.description ?? "—"}</td>
                <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{r.isSystem ? "Yes" : "No"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

"use client";

import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { cn } from "@/shared/utils/cn";
import type { UserSummaryDto } from "../types";
import {
  adminSetPassword,
  createUser,
  fetchRoles,
  fetchUserRoles,
  fetchUsers,
  setUserRoles,
  updateUser,
} from "../services/userManagementApi";

const inputClass =
  "mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100";

export function UsersSection() {
  const queryClient = useQueryClient();
  const usersQuery = useQuery({ queryKey: ["users"], queryFn: fetchUsers });
  const rolesQuery = useQuery({ queryKey: ["roles"], queryFn: fetchRoles });

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<UserSummaryDto | null>(null);
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [isActive, setIsActive] = useState(true);

  const [rolesUserId, setRolesUserId] = useState<number | null>(null);
  const [selectedRoleIds, setSelectedRoleIds] = useState<Set<number>>(new Set());

  const [passwordUserId, setPasswordUserId] = useState<number | null>(null);
  const [newPassword, setNewPassword] = useState("");

  const userRolesQuery = useQuery({
    queryKey: ["user-roles", rolesUserId],
    queryFn: () => fetchUserRoles(rolesUserId!),
    enabled: rolesUserId != null,
  });

  useEffect(() => {
    if (userRolesQuery.data) {
      setSelectedRoleIds(new Set(userRolesQuery.data.map((r) => r.id)));
    }
  }, [userRolesQuery.data]);

  const createMut = useMutation({
    mutationFn: createUser,
    onSuccess: async () => {
      toast.success("User created.");
      await queryClient.invalidateQueries({ queryKey: ["users"] });
      closeForm();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const updateMut = useMutation({
    mutationFn: ({ id, body }: { id: number; body: Parameters<typeof updateUser>[1] }) => updateUser(id, body),
    onSuccess: async () => {
      toast.success("User updated.");
      await queryClient.invalidateQueries({ queryKey: ["users"] });
      closeForm();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const setRolesMut = useMutation({
    mutationFn: ({ userId, roleIds }: { userId: number; roleIds: number[] }) => setUserRoles(userId, roleIds),
    onSuccess: async () => {
      toast.success("Roles updated.");
      await queryClient.invalidateQueries({ queryKey: ["user-roles", rolesUserId] });
      closeRolesModal();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const passwordMut = useMutation({
    mutationFn: ({ userId, pwd }: { userId: number; pwd: string }) => adminSetPassword(userId, { newPassword: pwd }),
    onSuccess: async () => {
      toast.success("Password updated.");
      closePasswordModal();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const openCreate = () => {
    setEditing(null);
    setEmail("");
    setUserName("");
    setPassword("");
    setIsActive(true);
    setFormOpen(true);
  };

  const openEdit = (u: UserSummaryDto) => {
    setEditing(u);
    setEmail(u.email);
    setUserName(u.userName ?? "");
    setPassword("");
    setIsActive(u.isActive);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
  };

  const openRolesModal = (u: UserSummaryDto) => {
    setRolesUserId(u.id);
    setSelectedRoleIds(new Set());
  };

  const closeRolesModal = () => {
    setRolesUserId(null);
  };

  const openPasswordModal = (u: UserSummaryDto) => {
    setPasswordUserId(u.id);
    setNewPassword("");
  };

  const closePasswordModal = () => {
    setPasswordUserId(null);
    setNewPassword("");
  };

  const toggleRole = (roleId: number, checked: boolean) => {
    setSelectedRoleIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(roleId);
      else next.delete(roleId);
      return next;
    });
  };

  const handleSubmitUser = async (e: React.FormEvent) => {
    e.preventDefault();
    const em = email.trim();
    if (!em) {
      toast.error("Email is required.");
      return;
    }
    if (!editing && !password.trim()) {
      toast.error("Password is required for new users.");
      return;
    }
    try {
      if (editing) {
        await updateMut.mutateAsync({
          id: editing.id,
          body: {
            email: em,
            userName: userName.trim() ? userName.trim() : null,
            isActive,
          },
        });
      } else {
        await createMut.mutateAsync({
          email: em,
          userName: userName.trim() ? userName.trim() : null,
          password: password,
          isActive,
        });
      }
    } catch {
      /* toast in mutation */
    }
  };

  const handleSaveRoles = () => {
    if (rolesUserId == null) return;
    setRolesMut.mutate({ userId: rolesUserId, roleIds: Array.from(selectedRoleIds) });
  };

  const handleSavePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (passwordUserId == null) return;
    if (!newPassword.trim()) {
      toast.error("Enter a new password.");
      return;
    }
    try {
      await passwordMut.mutateAsync({ userId: passwordUserId, pwd: newPassword });
    } catch {
      /* toast in mutation */
    }
  };

  const users = usersQuery.data ?? [];
  const allRoles = rolesQuery.data ?? [];

  const passwordUserLabel = useMemo(() => {
    if (passwordUserId == null) return "";
    const u = users.find((x) => x.id === passwordUserId);
    return u?.email ?? "";
  }, [passwordUserId, users]);

  const busyUser = createMut.isPending || updateMut.isPending;

  if (usersQuery.isLoading || rolesQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (usersQuery.isError || rolesQuery.isError) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {getErrorMessage(usersQuery.error ?? rolesQuery.error)}
      </p>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-zinc-50">Users</h1>
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            Admin-only. Passwords are never shown; use &quot;Set password&quot; to reset an account.
          </p>
        </div>
        <Button type="button" onClick={openCreate}>
          Add user
        </Button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-zinc-200 dark:border-zinc-700">
        <table className="min-w-full divide-y divide-zinc-200 text-left text-sm dark:divide-zinc-700">
          <thead className="bg-zinc-50 dark:bg-zinc-900/50">
            <tr>
              <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Email</th>
              <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Display name</th>
              <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Active</th>
              <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-200 dark:divide-zinc-700">
            {users.map((u) => (
              <tr key={u.id} className="bg-white hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900">
                <td className="px-4 py-3 text-zinc-900 dark:text-zinc-100">{u.email}</td>
                <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{u.userName ?? "—"}</td>
                <td className="px-4 py-3">
                  <span
                    className={cn(
                      "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                      u.isActive
                        ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200"
                        : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300",
                    )}
                  >
                    {u.isActive ? "Yes" : "No"}
                  </span>
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="secondary"
                      className="!py-1 !text-xs"
                      onClick={() => openEdit(u)}
                    >
                      Edit
                    </Button>
                    <Button
                      type="button"
                      variant="secondary"
                      className="!py-1 !text-xs"
                      onClick={() => openRolesModal(u)}
                    >
                      Roles
                    </Button>
                    <Button
                      type="button"
                      variant="secondary"
                      className="!py-1 !text-xs"
                      onClick={() => openPasswordModal(u)}
                    >
                      Set password
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {users.length === 0 ? (
          <p className="p-6 text-center text-sm text-zinc-500">No users yet.</p>
        ) : null}
      </div>

      <Modal
        open={formOpen}
        title={editing ? "Edit user" : "New user"}
        onClose={closeForm}
        className="max-w-lg"
      >
        <form onSubmit={(e) => void handleSubmitUser(e)} className="space-y-4">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Email
            </label>
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className={inputClass}
              required
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Display name
            </label>
            <input
              type="text"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              className={inputClass}
              placeholder="Optional"
            />
          </div>
          {!editing ? (
            <div>
              <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
                Password
              </label>
              <input
                type="password"
                autoComplete="new-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={inputClass}
                required={!editing}
              />
            </div>
          ) : null}
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
            <Button type="submit" disabled={busyUser}>
              {busyUser ? "Saving…" : editing ? "Save" : "Create"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        open={rolesUserId != null}
        title="Assign roles"
        onClose={closeRolesModal}
        className="max-w-md"
        footer={
          <>
            <Button type="button" variant="secondary" onClick={closeRolesModal}>
              Cancel
            </Button>
            <Button
              type="button"
              onClick={() => handleSaveRoles()}
              disabled={setRolesMut.isPending || userRolesQuery.isLoading}
            >
              {setRolesMut.isPending ? "Saving…" : "Save"}
            </Button>
          </>
        }
      >
        {rolesUserId != null && userRolesQuery.isLoading ? (
          <div className="flex justify-center py-8">
            <Spinner />
          </div>
        ) : (
          <ul className="max-h-[50vh] space-y-2 overflow-y-auto">
            {allRoles.map((r) => (
              <li key={r.id}>
                <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-zinc-200 px-3 py-2 dark:border-zinc-700">
                  <input
                    type="checkbox"
                    className="mt-0.5 size-4 rounded border-zinc-300"
                    checked={selectedRoleIds.has(r.id)}
                    onChange={(e) => toggleRole(r.id, e.target.checked)}
                  />
                  <span>
                    <span className="text-sm font-medium text-zinc-900 dark:text-zinc-100">{r.name}</span>
                    {r.description ? (
                      <span className="mt-0.5 block text-xs text-zinc-500">{r.description}</span>
                    ) : null}
                    {r.isSystem ? (
                      <span className="mt-0.5 block text-xs text-amber-700 dark:text-amber-400">System role</span>
                    ) : null}
                  </span>
                </label>
              </li>
            ))}
          </ul>
        )}
      </Modal>

      <Modal
        open={passwordUserId != null}
        title={`Set password${passwordUserLabel ? ` — ${passwordUserLabel}` : ""}`}
        onClose={closePasswordModal}
        className="max-w-md"
      >
        <form onSubmit={(e) => void handleSavePassword(e)} className="space-y-4">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              New password
            </label>
            <input
              type="password"
              autoComplete="new-password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className={inputClass}
              required
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" onClick={closePasswordModal}>
              Cancel
            </Button>
            <Button type="submit" disabled={passwordMut.isPending}>
              {passwordMut.isPending ? "Saving…" : "Save password"}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { ApiAvailabilityAlert } from "@/shared/components/ApiAvailabilityAlert";
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
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

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
  const [selectedRoleIds, setSelectedRoleIds] = useState<Set<number> | null>(null);

  const [passwordUserId, setPasswordUserId] = useState<number | null>(null);
  const [newPassword, setNewPassword] = useState("");

  const userRolesQuery = useQuery({
    queryKey: ["user-roles", rolesUserId],
    queryFn: () => fetchUserRoles(rolesUserId!),
    enabled: rolesUserId != null,
  });

  const effectiveSelectedRoleIds = useMemo(
    () => selectedRoleIds ?? new Set((userRolesQuery.data ?? []).map((r) => r.id)),
    [selectedRoleIds, userRolesQuery.data],
  );

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
    setSelectedRoleIds(null);
  };

  const closeRolesModal = () => {
    setRolesUserId(null);
    setSelectedRoleIds(null);
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
      const base = prev ?? new Set((userRolesQuery.data ?? []).map((r) => r.id));
      const next = new Set(base);
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
    setRolesMut.mutate({ userId: rolesUserId, roleIds: Array.from(effectiveSelectedRoleIds) });
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
    return <ApiAvailabilityAlert error={usersQuery.error ?? rolesQuery.error} />;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">Users</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Admin-only. Passwords are never shown; use &quot;Set password&quot; to reset an account.
          </p>
        </div>
        <Button type="button" onClick={openCreate}>
          Add user
        </Button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-border">
        <table className="min-w-full divide-y divide-border text-left text-sm ">
          <thead className="bg-muted/50">
            <tr>
              <th className="px-4 py-3 font-medium text-muted-foreground">Email</th>
              <th className="px-4 py-3 font-medium text-muted-foreground">Display name</th>
              <th className="px-4 py-3 font-medium text-muted-foreground">Active</th>
              <th className="px-4 py-3 font-medium text-muted-foreground">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {users.map((u) => (
              <tr key={u.id} className="bg-card hover:bg-muted/40">
                <td className="px-4 py-3 text-foreground">{u.email}</td>
                <td className="px-4 py-3 text-muted-foreground">{u.userName ?? "—"}</td>
                <td className="px-4 py-3">
                  <span
                    className={cn(
                      "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                      u.isActive
                        ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200"
                        : "bg-muted text-muted-foreground",
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
          <p className="p-6 text-center text-sm text-muted-foreground">No users yet.</p>
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
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
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
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
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
              <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
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
                <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-border px-3 py-2 dark:border-border">
                  <input
                    type="checkbox"
                    className="mt-0.5 size-4 rounded border-input"
                    checked={effectiveSelectedRoleIds.has(r.id)}
                    onChange={(e) => toggleRole(r.id, e.target.checked)}
                  />
                  <span>
                    <span className="text-sm font-medium text-foreground">{r.name}</span>
                    {r.description ? (
                      <span className="mt-0.5 block text-xs text-muted-foreground">{r.description}</span>
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
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
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

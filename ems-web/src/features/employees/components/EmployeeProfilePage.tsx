"use client";

import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { fetchRoles, fetchUsers } from "@/features/user-management/services/userManagementApi";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { cn } from "@/shared/utils/cn";
import {
  useEmployeeEffectiveRoles,
  useEmployeeHistory,
  useEmployeeProfile,
  useSetEmployeeDirectRoles,
} from "../hooks";
import { useEmployeeCapabilities } from "../hooks/useEmployeeCapabilities";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";
import type {
  Employee,
  EmployeeHistoryResponse,
  EmployeeProfile,
  EmploymentStatusHistoryItem,
} from "../types/employee.types";
import { EmploymentStatus, employmentStatusLabels } from "../types/employment-status";
import { toDateInputValue } from "../utils/date-format";
import { EmployeeForm } from "./EmployeeForm";
import {
  ArchiveEmployeeDialog,
  ChangeEmploymentStatusDialog,
  RestoreEmployeeDialog,
  TerminateEmployeeDialog,
} from "./EmployeeLifecycleDialogs";
import { EmploymentStatusPill } from "./EmploymentStatusPill";
import { EmployeeTransferDialog } from "./EmployeeTransferDialog";
import { EmployeeUpcomingChanges } from "./EmployeeUpcomingChanges";
import { EmployeeInvitationPanel } from "./EmployeeInvitationPanel";

type ProfileTab = "overview" | "employment" | "history" | "access" | "documents";

type EmployeeProfilePageProps = {
  employeeId: number;
};

const tabs: { id: ProfileTab; label: string }[] = [
  { id: "overview", label: "Overview" },
  { id: "employment", label: "Employment" },
  { id: "history", label: "History" },
  { id: "access", label: "Access" },
  { id: "documents", label: "Documents" },
];

/** Maps profile API shape to Employee for EmployeeForm edit mode. */
export function profileToEmployee(profile: EmployeeProfile): Employee {
  return {
    id: profile.id,
    organizationId: profile.organizationId,
    departmentId: profile.departmentId,
    locationId: profile.locationId,
    managerId: profile.managerId,
    jobPositionId: profile.jobPositionId,
    employeeNumber: profile.employeeNumber,
    firstName: profile.firstName,
    lastName: profile.lastName,
    email: profile.email,
    phoneNumber: profile.phoneNumber,
    dateOfBirth: profile.dateOfBirth,
    dateJoined: profile.dateJoined,
    employmentStatus: profile.employmentStatus,
    isActive: profile.isActive,
    isArchived: profile.isArchived,
    archivedAtUtc: profile.archivedAtUtc,
    retentionUntilUtc: profile.retentionUntilUtc,
    archiveReason: profile.archiveReason,
    createdAtUtc: profile.createdAtUtc,
    updatedAtUtc: profile.updatedAtUtc,
  };
}

export function EmployeeProfilePage({ employeeId }: EmployeeProfilePageProps) {
  const queryClient = useQueryClient();
  const { capabilities, isLoading: capabilitiesLoading } = useEmployeeCapabilities();
  const [activeTab, setActiveTab] = useState<ProfileTab>("overview");
  const [editOpen, setEditOpen] = useState(false);
  const [transferOpen, setTransferOpen] = useState(false);
  const [changeStatusOpen, setChangeStatusOpen] = useState(false);
  const [terminateOpen, setTerminateOpen] = useState(false);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [restoreOpen, setRestoreOpen] = useState(false);
  const [provisionConfirmOpen, setProvisionConfirmOpen] = useState(false);
  const [linkUserOpen, setLinkUserOpen] = useState(false);
  const [linkUserId, setLinkUserId] = useState<number | null>(null);
  const [linkConfirmOpen, setLinkConfirmOpen] = useState(false);
  const [directRolesOpen, setDirectRolesOpen] = useState(false);
  const [selectedDirectRoleIds, setSelectedDirectRoleIds] = useState<Set<number>>(new Set());

  const profileQuery = useEmployeeProfile(employeeId);
  const historyQuery = useEmployeeHistory(employeeId);
  const rolesQuery = useEmployeeEffectiveRoles(employeeId);
  const setDirectRolesMutation = useSetEmployeeDirectRoles();

  const allRolesQuery = useQuery({
    queryKey: ["roles"],
    queryFn: fetchRoles,
    enabled: activeTab === "access" && capabilities.access,
  });
  const usersQuery = useQuery({
    queryKey: ["users"],
    queryFn: fetchUsers,
    enabled: activeTab === "access" && capabilities.access,
  });

  const profile = profileQuery.data;
  const employeeForForm = profile ? profileToEmployee(profile) : null;

  const invalidateEmployee = () => {
    void queryClient.invalidateQueries({ queryKey: employeeKeys.profile(employeeId) });
    void queryClient.invalidateQueries({ queryKey: employeeKeys.history(employeeId) });
    void queryClient.invalidateQueries({ queryKey: employeeKeys.detail(employeeId) });
    void queryClient.invalidateQueries({ queryKey: employeeKeys.effectiveRoles(employeeId) });
    void queryClient.invalidateQueries({ queryKey: employeeKeys.all });
  };

  const timeline = useMemo(
    () => (historyQuery.data ? buildMergedTimeline(historyQuery.data) : []),
    [historyQuery.data],
  );

  const directRoles = useMemo(
    () => (rolesQuery.data ?? []).filter((r) => r.source === "direct_override"),
    [rolesQuery.data],
  );

  const inheritedRoles = useMemo(
    () => (rolesQuery.data ?? []).filter((r) => r.source === "position_inherited"),
    [rolesQuery.data],
  );

  const openDirectRolesEditor = () => {
    setSelectedDirectRoleIds(new Set(directRoles.map((r) => r.roleId)));
    setDirectRolesOpen(true);
  };

  const handleProvision = () => {
    sendInvitationMut.mutate(undefined, {
      onSuccess: () => {
        setProvisionConfirmOpen(false);
        invalidateEmployee();
      },
    });
  };

  const sendInvitationMut = useMutation({
    mutationFn: () => employeeService.sendInvitation(employeeId),
    onSuccess: () => toast.success("Invitation email sent."),
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const reactivateLoginMut = useMutation({
    mutationFn: () => employeeService.reactivateLogin(employeeId),
    onSuccess: () => {
      toast.success("Login reactivated.");
      invalidateEmployee();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const handleLinkUser = async () => {
    if (linkUserId == null) return;
    try {
      await employeeService.linkEmployeeUser(employeeId, { userId: linkUserId });
      toast.success("Login linked to employee.");
      setLinkConfirmOpen(false);
      setLinkUserOpen(false);
      setLinkUserId(null);
      invalidateEmployee();
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  const handleSaveDirectRoles = () => {
    setDirectRolesMutation.mutate(
      { employeeId, roleIds: Array.from(selectedDirectRoleIds) },
      {
        onSuccess: () => {
          toast.success("Direct role overrides updated.");
          setDirectRolesOpen(false);
          invalidateEmployee();
        },
        onError: (e) => toast.error(getErrorMessage(e)),
      },
    );
  };

  if (capabilitiesLoading || profileQuery.isLoading) {
    return (
      <div className="flex justify-center py-20">
        <Spinner />
      </div>
    );
  }

  if (profileQuery.isError || !profile) {
    return (
      <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-6 text-center">
        <p className="text-sm text-destructive">Could not load employee profile.</p>
        <p className="mt-1 text-xs text-muted-foreground">{getErrorMessage(profileQuery.error)}</p>
        <Link href="/employees" className="mt-4 inline-block text-sm text-primary hover:underline">
          Back to employees
        </Link>
      </div>
    );
  }

  if (!capabilities.view) {
    return (
      <div className="rounded-xl border border-border bg-card p-8 text-center">
        <p className="text-sm text-muted-foreground">You do not have permission to view this employee.</p>
        <Link href="/employees" className="mt-4 inline-block text-sm text-primary hover:underline">
          Back to employees
        </Link>
      </div>
    );
  }

  const showLifecycleActions = !profile.isArchived && capabilities.manage;
  const showRestore = profile.isArchived && capabilities.manage;
  const showTerminate =
    !profile.isArchived && profile.employmentStatus !== EmploymentStatus.Terminated && capabilities.manage;
  const showChangeStatus =
    !profile.isArchived && profile.employmentStatus !== EmploymentStatus.Terminated && capabilities.manage;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-2">
          <Link href="/employees" className="text-sm text-muted-foreground hover:text-primary hover:underline">
            ← Employees
          </Link>
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-2xl font-semibold text-foreground">
              {profile.firstName} {profile.lastName}
            </h1>
            <EmploymentStatusPill status={profile.employmentStatus} />
            {profile.isArchived ? (
              <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
                Archived
              </span>
            ) : null}
          </div>
          <p className="font-mono text-sm text-muted-foreground">{profile.employeeNumber}</p>
          <dl className="grid gap-x-6 gap-y-1 text-sm sm:grid-cols-2">
            <MetaItem label="Position" value={profile.jobPositionTitle ?? "—"} />
            <MetaItem label="Department" value={profile.departmentName ?? "—"} />
            <MetaItem
              label="Manager"
              value={
                profile.managerName
                  ? `${profile.managerName}${profile.managerEmployeeNumber ? ` (${profile.managerEmployeeNumber})` : ""}`
                  : "No manager"
              }
            />
            <MetaItem label="Primary site" value={profile.primarySiteName ?? "—"} />
          </dl>
        </div>

        <div className="flex flex-wrap gap-2">
          {showChangeStatus ? (
            <Button type="button" variant="secondary" onClick={() => setChangeStatusOpen(true)}>
              Change status
            </Button>
          ) : null}
          {showTerminate ? (
            <Button type="button" variant="secondary" onClick={() => setTerminateOpen(true)}>
              Terminate
            </Button>
          ) : null}
          {showLifecycleActions ? (
            <Button type="button" variant="danger" onClick={() => setArchiveOpen(true)}>
              Archive
            </Button>
          ) : null}
          {showRestore ? (
            <Button type="button" onClick={() => setRestoreOpen(true)}>
              Restore
            </Button>
          ) : null}
        </div>
      </div>

      <div className="flex flex-wrap gap-2 border-b border-border pb-2">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => setActiveTab(tab.id)}
            className={cn(
              "rounded-lg px-3 py-1.5 text-sm font-medium transition-colors",
              activeTab === tab.id
                ? "bg-primary/10 text-primary"
                : "text-muted-foreground hover:bg-muted hover:text-foreground",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "overview" ? (
        <section className="space-y-4 rounded-xl border border-border p-6">
          <div className="flex items-center justify-between gap-4">
            <h2 className="text-lg font-semibold text-foreground">Contact information</h2>
            {!profile.isArchived && capabilities.manage ? (
              <Button type="button" variant="secondary" onClick={() => setEditOpen(true)}>
                Edit profile
              </Button>
            ) : null}
          </div>
          <dl className="grid gap-4 sm:grid-cols-2">
            <DetailItem label="Email" value={profile.email} />
            <DetailItem label="Phone" value={profile.phoneNumber ?? "—"} />
            <DetailItem label="Date of birth" value={toDateInputValue(profile.dateOfBirth) || "—"} />
            <DetailItem label="Date joined" value={toDateInputValue(profile.dateJoined) || "—"} />
            <DetailItem label="Address location" value={profile.locationLabel ?? "—"} />
            <DetailItem label="Primary site" value={profile.primarySiteName ?? "—"} />
          </dl>
          <div className="border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-foreground">Upcoming changes</h3>
            <div className="mt-3">
              <EmployeeUpcomingChanges employeeId={employeeId} canManage={capabilities.manage} />
            </div>
          </div>
        </section>
      ) : null}

      {activeTab === "employment" ? (
        <section className="space-y-4 rounded-xl border border-border p-6">
          <div className="flex items-center justify-between gap-4">
            <h2 className="text-lg font-semibold text-foreground">Organization assignment</h2>
            {!profile.isArchived && capabilities.manage ? (
              <Button type="button" onClick={() => setTransferOpen(true)}>
                Transfer
              </Button>
            ) : null}
          </div>
          <dl className="grid gap-4 sm:grid-cols-2">
            <DetailItem label="Department" value={profile.departmentName ?? "—"} />
            <DetailItem
              label="Position"
              value={
                profile.jobPositionTitle
                  ? profile.jobPositionCode
                    ? `${profile.jobPositionTitle} (${profile.jobPositionCode})`
                    : profile.jobPositionTitle
                  : "—"
              }
            />
            <DetailItem
              label="Manager"
              value={
                profile.managerName
                  ? `${profile.managerName}${profile.managerEmployeeNumber ? ` (${profile.managerEmployeeNumber})` : ""}`
                  : "No manager"
              }
            />
            <DetailItem label="Employment status" value={employmentStatusLabels[profile.employmentStatus]} />
            <DetailItem label="Date joined" value={toDateInputValue(profile.dateJoined) || "—"} />
            <DetailItem label="Address location" value={profile.locationLabel ?? "—"} />
          </dl>
          <div>
            <h3 className="text-sm font-medium text-foreground">Sites</h3>
            {profile.siteNames.length > 0 ? (
              <ul className="mt-2 list-inside list-disc text-sm text-muted-foreground">
                {profile.siteNames.map((site) => (
                  <li key={site}>{site}</li>
                ))}
              </ul>
            ) : (
              <p className="mt-1 text-sm text-muted-foreground">No sites assigned.</p>
            )}
          </div>
          {profile.isArchived ? (
            <div className="rounded-lg border border-border bg-muted/40 p-4 text-sm">
              <p className="font-medium text-foreground">Archived</p>
              {profile.archivedAtUtc ? (
                <p className="mt-1 text-muted-foreground">
                  Archived on {new Date(profile.archivedAtUtc).toLocaleString()}
                </p>
              ) : null}
              {profile.archiveReason ? (
                <p className="mt-1 text-muted-foreground">Reason: {profile.archiveReason}</p>
              ) : null}
            </div>
          ) : null}
        </section>
      ) : null}

      {activeTab === "history" ? (
        <section className="space-y-4 rounded-xl border border-border p-6">
          <h2 className="text-lg font-semibold text-foreground">Employment history</h2>
          {historyQuery.isLoading ? (
            <div className="flex justify-center py-12">
              <Spinner />
            </div>
          ) : historyQuery.isError ? (
            <p className="text-sm text-destructive">{getErrorMessage(historyQuery.error)}</p>
          ) : timeline.length === 0 ? (
            <p className="text-sm text-muted-foreground">No history recorded yet.</p>
          ) : (
            <ol className="relative space-y-4 border-l border-border pl-6">
              {timeline.map((entry) => (
                <li key={entry.id} className="relative">
                  <span className="absolute -left-[1.6rem] top-1.5 size-2.5 rounded-full bg-primary" />
                  <p className="text-sm font-medium text-foreground">{entry.summary}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Effective {new Date(entry.effectiveAt).toLocaleString()}
                    {entry.reason ? ` · ${entry.reason}` : ""}
                  </p>
                  <p className="text-xs text-muted-foreground">By {entry.actor}</p>
                </li>
              ))}
            </ol>
          )}
        </section>
      ) : null}

      {activeTab === "access" ? (
        <section className="space-y-6 rounded-xl border border-border p-6">
          <div>
            <h2 className="text-lg font-semibold text-foreground">Linked login</h2>
            <dl className="mt-4 grid gap-4 sm:grid-cols-2">
              <DetailItem
                label="Status"
                value={
                  profile.hasLinkedLogin
                    ? profile.linkedLoginIsActive
                      ? "Linked (active)"
                      : "Linked (disabled)"
                    : "Not linked"
                }
              />
              <DetailItem label="Login email" value={profile.linkedUserEmail ?? "—"} />
            </dl>
            {profile.isArchived ? (
              <p className="mt-3 text-sm text-muted-foreground">
                Restoring an archived employee record does not re-enable login access. Use Reactivate login after restore when appropriate.
              </p>
            ) : null}
            {!profile.isArchived && profile.hasLinkedLogin && profile.linkedLoginIsActive === false && capabilities.access ? (
              <div className="mt-4">
                <Button
                  type="button"
                  variant="secondary"
                  disabled={reactivateLoginMut.isPending || profile.employmentStatus !== EmploymentStatus.Active}
                  onClick={() => reactivateLoginMut.mutate()}
                >
                  Reactivate login
                </Button>
              </div>
            ) : null}
            {!profile.isArchived && capabilities.access ? (
              <div className="mt-4 space-y-4">
                <EmployeeInvitationPanel
                  employeeId={employeeId}
                  hasLinkedLogin={profile.hasLinkedLogin}
                  linkedLoginIsActive={profile.linkedLoginIsActive}
                />
                {!profile.hasLinkedLogin ? (
                  <div className="flex flex-wrap gap-2">
                    <Button type="button" variant="secondary" onClick={() => setProvisionConfirmOpen(true)}>
                      Send login invitation
                    </Button>
                    <Button type="button" variant="secondary" onClick={() => setLinkUserOpen(true)}>
                      Link existing user
                    </Button>
                  </div>
                ) : null}
              </div>
            ) : null}
          </div>

          <div>
            <div className="flex items-center justify-between gap-4">
              <h2 className="text-lg font-semibold text-foreground">Effective roles</h2>
              {!profile.isArchived && profile.hasLinkedLogin && capabilities.access ? (
                <Button type="button" variant="secondary" size="sm" onClick={openDirectRolesEditor}>
                  Edit direct roles
                </Button>
              ) : null}
            </div>
            {rolesQuery.isLoading ? (
              <div className="flex justify-center py-8">
                <Spinner />
              </div>
            ) : rolesQuery.isError ? (
              <p className="mt-2 text-sm text-destructive">{getErrorMessage(rolesQuery.error)}</p>
            ) : (
              <div className="mt-4 space-y-4">
                <RoleGroup title="Inherited from position" roles={inheritedRoles} emptyLabel="No inherited roles." />
                <RoleGroup title="Direct overrides" roles={directRoles} emptyLabel="No direct role overrides." />
              </div>
            )}
          </div>
        </section>
      ) : null}

      {activeTab === "documents" ? (
        <section className="rounded-xl border border-dashed border-border p-10 text-center">
          <p className="text-lg font-medium text-foreground">Documents</p>
          <p className="mt-1 text-sm text-muted-foreground">
            Employee document management will be available in a future release.
          </p>
        </section>
      ) : null}

      <Modal open={editOpen} title="Edit employee profile" onClose={() => setEditOpen(false)} className="max-w-3xl">
        {employeeForForm ? (
          <EmployeeForm
            mode="edit"
            employee={employeeForForm}
            onSuccess={() => {
              setEditOpen(false);
              invalidateEmployee();
            }}
            onCancel={() => setEditOpen(false)}
          />
        ) : null}
      </Modal>

      <EmployeeTransferDialog
        open={transferOpen}
        profile={profile}
        onClose={() => setTransferOpen(false)}
        onTransferred={invalidateEmployee}
      />

      <ChangeEmploymentStatusDialog
        open={changeStatusOpen}
        profile={profile}
        onClose={() => setChangeStatusOpen(false)}
        onSuccess={invalidateEmployee}
      />
      <TerminateEmployeeDialog
        open={terminateOpen}
        profile={profile}
        onClose={() => setTerminateOpen(false)}
        onSuccess={invalidateEmployee}
      />
      <ArchiveEmployeeDialog
        open={archiveOpen}
        profile={profile}
        onClose={() => setArchiveOpen(false)}
        onSuccess={invalidateEmployee}
      />
      <RestoreEmployeeDialog
        open={restoreOpen}
        profile={profile}
        onClose={() => setRestoreOpen(false)}
        onSuccess={invalidateEmployee}
      />

      <Modal
        open={provisionConfirmOpen}
        title="Provision login"
        onClose={() => setProvisionConfirmOpen(false)}
        footer={
          <>
            <Button
              type="button"
              variant="secondary"
              onClick={() => setProvisionConfirmOpen(false)}
              disabled={sendInvitationMut.isPending}
            >
              Cancel
            </Button>
            <Button type="button" onClick={handleProvision} disabled={sendInvitationMut.isPending}>
              {sendInvitationMut.isPending ? "Sending…" : "Send invitation"}
            </Button>
          </>
        }
      >
        <p className="text-sm text-muted-foreground">
          Create or link a sign-in for <strong className="text-foreground">{profile.email}</strong> and send an
          invitation? Temporary passwords are not shown here for security.
        </p>
      </Modal>

      <Modal
        open={linkUserOpen}
        title="Link existing user"
        onClose={() => {
          setLinkUserOpen(false);
          setLinkUserId(null);
        }}
        footer={
          <>
            <Button type="button" variant="secondary" onClick={() => setLinkUserOpen(false)}>
              Cancel
            </Button>
            <Button
              type="button"
              disabled={linkUserId == null}
              onClick={() => {
                setLinkUserOpen(false);
                setLinkConfirmOpen(true);
              }}
            >
              Continue
            </Button>
          </>
        }
      >
        <div className="space-y-2">
          <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">User account</label>
          {usersQuery.isLoading ? (
            <Spinner />
          ) : (
            <select
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
              value={linkUserId ?? ""}
              onChange={(e) => setLinkUserId(e.target.value ? Number(e.target.value) : null)}
            >
              <option value="">Select user…</option>
              {(usersQuery.data ?? []).map((user) => (
                <option key={user.id} value={user.id}>
                  {user.email} {user.userName ? `(${user.userName})` : ""}
                </option>
              ))}
            </select>
          )}
        </div>
      </Modal>

      <Modal
        open={linkConfirmOpen}
        title="Confirm link login"
        onClose={() => setLinkConfirmOpen(false)}
        footer={
          <>
            <Button type="button" variant="secondary" onClick={() => setLinkConfirmOpen(false)}>
              Cancel
            </Button>
            <Button type="button" onClick={() => void handleLinkUser()}>
              Link login
            </Button>
          </>
        }
      >
        <p className="text-sm text-muted-foreground">
          Link the selected user account to{" "}
          <strong className="text-foreground">
            {profile.firstName} {profile.lastName}
          </strong>
          ?
        </p>
      </Modal>

      <Modal
        open={directRolesOpen}
        title="Edit direct role overrides"
        onClose={() => setDirectRolesOpen(false)}
        className="max-w-lg"
        footer={
          <>
            <Button
              type="button"
              variant="secondary"
              onClick={() => setDirectRolesOpen(false)}
              disabled={setDirectRolesMutation.isPending}
            >
              Cancel
            </Button>
            <Button type="button" onClick={handleSaveDirectRoles} disabled={setDirectRolesMutation.isPending}>
              {setDirectRolesMutation.isPending ? "Saving…" : "Save roles"}
            </Button>
          </>
        }
      >
        {allRolesQuery.isLoading ? (
          <Spinner />
        ) : (
          <ul className="max-h-64 space-y-2 overflow-y-auto">
            {(allRolesQuery.data ?? []).map((role) => (
              <li key={role.id}>
                <label className="flex cursor-pointer items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={selectedDirectRoleIds.has(role.id)}
                    onChange={() => {
                      setSelectedDirectRoleIds((prev) => {
                        const next = new Set(prev);
                        if (next.has(role.id)) next.delete(role.id);
                        else next.add(role.id);
                        return next;
                      });
                    }}
                  />
                  <span>{role.name}</span>
                  {role.description ? (
                    <span className="text-xs text-muted-foreground">— {role.description}</span>
                  ) : null}
                </label>
              </li>
            ))}
          </ul>
        )}
      </Modal>
    </div>
  );
}

type TimelineEntry = {
  id: string;
  effectiveAt: string;
  createdAt: string;
  summary: string;
  reason: string | null;
  actor: string;
};

function actorLabel(item: {
  changedByUserName: string | null;
  changedByEmail: string | null;
}): string {
  if (item.changedByUserName) return item.changedByUserName;
  if (item.changedByEmail) return item.changedByEmail;
  return "System";
}

function statusLabel(status: EmploymentStatusHistoryItem["newStatus"] | null): string {
  if (status == null) return "—";
  return employmentStatusLabels[status] ?? String(status);
}

function buildMergedTimeline(history: EmployeeHistoryResponse): TimelineEntry[] {
  const entries: TimelineEntry[] = [];

  for (const item of history.positionHistory) {
    entries.push({
      id: `position-${item.id}`,
      effectiveAt: item.effectiveFromUtc,
      createdAt: item.createdAtUtc,
      summary: `Position: ${item.previousJobPositionTitle ?? "—"} → ${item.newJobPositionTitle ?? "—"}`,
      reason: item.reason,
      actor: actorLabel(item),
    });
  }

  for (const item of history.departmentHistory) {
    entries.push({
      id: `department-${item.id}`,
      effectiveAt: item.effectiveFromUtc,
      createdAt: item.createdAtUtc,
      summary: `Department: ${item.previousDepartmentName ?? "—"} → ${item.newDepartmentName ?? "—"}`,
      reason: item.reason,
      actor: actorLabel(item),
    });
  }

  for (const item of history.managerHistory) {
    const prev = item.previousManagerName
      ? `${item.previousManagerName}${item.previousManagerEmployeeNumber ? ` (${item.previousManagerEmployeeNumber})` : ""}`
      : "No manager";
    const next = item.newManagerName
      ? `${item.newManagerName}${item.newManagerEmployeeNumber ? ` (${item.newManagerEmployeeNumber})` : ""}`
      : "No manager";
    entries.push({
      id: `manager-${item.id}`,
      effectiveAt: item.effectiveFromUtc,
      createdAt: item.createdAtUtc,
      summary: `Manager: ${prev} → ${next}`,
      reason: item.reason,
      actor: actorLabel(item),
    });
  }

  for (const item of history.employmentStatusHistory) {
    entries.push({
      id: `status-${item.id}`,
      effectiveAt: item.effectiveDateUtc,
      createdAt: item.createdAtUtc,
      summary: `Status: ${statusLabel(item.previousStatus)} → ${statusLabel(item.newStatus)}`,
      reason: item.reason,
      actor: actorLabel(item),
    });
  }

  return entries.sort((a, b) => {
    const effectiveDiff = new Date(b.effectiveAt).getTime() - new Date(a.effectiveAt).getTime();
    if (effectiveDiff !== 0) return effectiveDiff;
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  });
}

function MetaItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{value}</dd>
    </div>
  );
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="mt-1 text-sm text-foreground">{value}</dd>
    </div>
  );
}

function RoleGroup({
  title,
  roles,
  emptyLabel,
}: {
  title: string;
  roles: { roleName: string; jobPositionTitle: string | null; isSystem: boolean }[];
  emptyLabel: string;
}) {
  return (
    <div>
      <h3 className="text-sm font-medium text-foreground">{title}</h3>
      {roles.length === 0 ? (
        <p className="mt-1 text-sm text-muted-foreground">{emptyLabel}</p>
      ) : (
        <ul className="mt-2 space-y-1">
          {roles.map((role) => (
            <li key={`${title}-${role.roleName}`} className="text-sm text-muted-foreground">
              {role.roleName}
              {role.jobPositionTitle ? ` (via ${role.jobPositionTitle})` : ""}
              {role.isSystem ? " · system" : ""}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

"use client";

import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { getErrorMessage } from "@/shared/api/http-client";
import { employeeService } from "../services/employeeService";
import type { EmployeeProfile } from "../types/employee.types";
import { EmploymentStatus, employmentStatusLabels, type EmploymentStatusValue } from "../types/employment-status";
import { dateInputToApiIso } from "../utils/date-format";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

type LifecycleDialogBaseProps = {
  open: boolean;
  profile: EmployeeProfile | null;
  onClose: () => void;
  onSuccess: () => void;
};

const changeableStatuses: EmploymentStatusValue[] = [
  EmploymentStatus.Preboarding,
  EmploymentStatus.Active,
  EmploymentStatus.Inactive,
];

export function ChangeEmploymentStatusDialog({ open, profile, onClose, onSuccess }: LifecycleDialogBaseProps) {
  return (
    <Modal
      open={open}
      title="Change employment status"
      onClose={onClose}
      footer={null}
    >
      {open && profile ? (
        <ChangeEmploymentStatusForm
          key={profile.id}
          profile={profile}
          onClose={onClose}
          onSuccess={onSuccess}
        />
      ) : null}
    </Modal>
  );
}

function ChangeEmploymentStatusForm({
  profile,
  onClose,
  onSuccess,
}: {
  profile: EmployeeProfile;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [newStatus, setNewStatus] = useState<EmploymentStatusValue>(
    profile.employmentStatus === EmploymentStatus.Terminated
      ? EmploymentStatus.Inactive
      : profile.employmentStatus,
  );
  const [effectiveDate, setEffectiveDate] = useState(new Date().toISOString().slice(0, 10));
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      employeeService.changeEmploymentStatus(profile.id, {
        newStatus,
        effectiveDateUtc: dateInputToApiIso(effectiveDate),
        reason: reason.trim() || null,
      }),
    onSuccess: () => {
      toast.success("Employment status updated.");
      onSuccess();
      onClose();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  return (
    <>
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Update status for{" "}
          <strong className="text-foreground">
            {profile.firstName} {profile.lastName}
          </strong>{" "}
          ({profile.employeeNumber}). Current status:{" "}
          <strong className="text-foreground">{employmentStatusLabels[profile.employmentStatus]}</strong>.
        </p>
        <div>
          <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
            New status
          </label>
          <select
            className={inputClass}
            value={newStatus}
            onChange={(e) => setNewStatus(Number(e.target.value) as EmploymentStatusValue)}
          >
            {changeableStatuses.map((status) => (
              <option key={status} value={status}>
                {employmentStatusLabels[status]}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Effective date
          </label>
          <input
            type="date"
            className={inputClass}
            value={effectiveDate}
            onChange={(e) => setEffectiveDate(e.target.value)}
          />
        </div>
        <div>
          <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Reason (optional)
          </label>
          <textarea className={inputClass} rows={3} value={reason} onChange={(e) => setReason(e.target.value)} />
        </div>
      </div>
      <div className="mt-6 flex flex-wrap justify-end gap-2 border-t border-border pt-4">
        <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
          Cancel
        </Button>
        <Button type="button" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? "Saving…" : "Update status"}
        </Button>
      </div>
    </>
  );
}

export function TerminateEmployeeDialog({ open, profile, onClose, onSuccess }: LifecycleDialogBaseProps) {
  return (
    <Modal open={open} title="Terminate employment" onClose={onClose} footer={null}>
      {open && profile ? (
        <TerminateEmployeeForm key={profile.id} profile={profile} onClose={onClose} onSuccess={onSuccess} />
      ) : null}
    </Modal>
  );
}

function TerminateEmployeeForm({
  profile,
  onClose,
  onSuccess,
}: {
  profile: EmployeeProfile;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [effectiveDate, setEffectiveDate] = useState(new Date().toISOString().slice(0, 10));
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      employeeService.terminateEmployee(profile.id, {
        effectiveDateUtc: dateInputToApiIso(effectiveDate),
        reason: reason.trim(),
      }),
    onSuccess: () => {
      toast.success("Employee terminated.");
      onSuccess();
      onClose();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const canSubmit = reason.trim().length > 0;

  return (
    <>
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Terminate{" "}
          <strong className="text-foreground">
            {profile.firstName} {profile.lastName}
          </strong>{" "}
          ({profile.employeeNumber})? This sets employment status to Terminated.
        </p>
        <div>
          <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Effective date
          </label>
          <input
            type="date"
            className={inputClass}
            value={effectiveDate}
            onChange={(e) => setEffectiveDate(e.target.value)}
          />
        </div>
        <div>
          <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Reason (required)
          </label>
          <textarea
            className={inputClass}
            rows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Reason for termination"
          />
        </div>
      </div>
      <div className="mt-6 flex flex-wrap justify-end gap-2 border-t border-border pt-4">
        <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
          Cancel
        </Button>
        <Button
          type="button"
          variant="danger"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending || !canSubmit}
        >
          {mutation.isPending ? "Terminating…" : "Terminate"}
        </Button>
      </div>
    </>
  );
}

export function ArchiveEmployeeDialog({ open, profile, onClose, onSuccess }: LifecycleDialogBaseProps) {
  return (
    <Modal open={open} title="Archive employee" onClose={onClose} footer={null}>
      {open && profile ? (
        <ArchiveEmployeeForm key={profile.id} profile={profile} onClose={onClose} onSuccess={onSuccess} />
      ) : null}
    </Modal>
  );
}

function ArchiveEmployeeForm({
  profile,
  onClose,
  onSuccess,
}: {
  profile: EmployeeProfile;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: () => employeeService.archiveEmployee(profile.id, { reason: reason.trim() }),
    onSuccess: () => {
      toast.success("Employee archived.");
      onSuccess();
      onClose();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const canSubmit = reason.trim().length > 0;

  return (
    <>
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Archive{" "}
          <strong className="text-foreground">
            {profile.firstName} {profile.lastName}
          </strong>{" "}
          ({profile.email})? The employee will disappear from active lists but remains retained according to your
          organization&apos;s policy.
        </p>
        <div>
          <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Reason (required)
          </label>
          <textarea
            className={inputClass}
            rows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Reason for archiving"
          />
        </div>
      </div>
      <div className="mt-6 flex flex-wrap justify-end gap-2 border-t border-border pt-4">
        <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
          Cancel
        </Button>
        <Button
          type="button"
          variant="danger"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending || !canSubmit}
        >
          {mutation.isPending ? "Archiving…" : "Archive"}
        </Button>
      </div>
    </>
  );
}

export function RestoreEmployeeDialog({ open, profile, onClose, onSuccess }: LifecycleDialogBaseProps) {
  const mutation = useMutation({
    mutationFn: () => employeeService.restoreEmployee(profile!.id),
    onSuccess: () => {
      toast.success("Employee restored.");
      onSuccess();
      onClose();
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  return (
    <Modal
      open={open}
      title="Restore employee"
      onClose={onClose}
      footer={
        <>
          <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancel
          </Button>
          <Button type="button" onClick={() => mutation.mutate()} disabled={mutation.isPending || !profile}>
            {mutation.isPending ? "Restoring…" : "Restore"}
          </Button>
        </>
      }
    >
      {profile ? (
        <p className="text-sm text-muted-foreground">
          Restore{" "}
          <strong className="text-foreground">
            {profile.firstName} {profile.lastName}
          </strong>{" "}
          ({profile.employeeNumber}) to the active employee directory?
        </p>
      ) : null}
    </Modal>
  );
}

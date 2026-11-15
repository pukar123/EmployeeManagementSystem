"use client";

import { useState } from "react";
import { toast } from "sonner";
import { leaveRequestStatusLabel } from "@/features/leave/types/leave.types";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { useManagerLeaveApprovals } from "../hooks";
import type { ManagerLeaveApprovalItem } from "../types/manager-team.types";

const MAX_REASON_LENGTH = 1000;

function unitLabel(unit: 1 | 2): string {
  return unit === 2 ? "hours" : "days";
}

type Props = {
  organizationId: number;
  /** Admins viewing another manager's team; omit for the signed-in manager. */
  managerId?: number | null;
};

export function ManagerLeaveApprovalsPanel({ organizationId, managerId }: Props) {
  const { approvalsQuery, approve, reject } = useManagerLeaveApprovals({ organizationId, managerId });
  const [rowErrors, setRowErrors] = useState<Record<number, string>>({});
  const [rejecting, setRejecting] = useState<ManagerLeaveApprovalItem | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejectError, setRejectError] = useState<string | null>(null);

  const setRowError = (id: number, message: string | null) =>
    setRowErrors((prev) => {
      const next = { ...prev };
      if (message) next[id] = message;
      else delete next[id];
      return next;
    });

  const handleApprove = async (item: ManagerLeaveApprovalItem) => {
    setRowError(item.leaveRequestId, null);
    try {
      await approve.mutateAsync({ id: item.leaveRequestId });
      toast.success(`Approved leave for ${item.employeeName}.`);
    } catch (e) {
      setRowError(item.leaveRequestId, getErrorMessage(e));
    }
  };

  const openReject = (item: ManagerLeaveApprovalItem) => {
    setRowError(item.leaveRequestId, null);
    setRejecting(item);
    setRejectReason("");
    setRejectError(null);
  };

  const closeReject = () => {
    setRejecting(null);
    setRejectReason("");
    setRejectError(null);
  };

  const handleReject = async () => {
    if (!rejecting) return;
    const reason = rejectReason.trim();
    if (!reason) {
      setRejectError("A reason is required to reject a leave request.");
      return;
    }
    try {
      await reject.mutateAsync({ id: rejecting.leaveRequestId, reason });
      toast.success(`Rejected leave for ${rejecting.employeeName}.`);
      closeReject();
    } catch (e) {
      setRejectError(getErrorMessage(e));
    }
  };

  const { data: items = [], isLoading, isError, error } = approvalsQuery;
  const busy = approve.isPending || reject.isPending;

  return (
    <section className="rounded-xl border border-border bg-card p-4 shadow-sm" aria-labelledby="pending-leave-approvals">
      <div className="flex items-center justify-between gap-3">
        <h2 id="pending-leave-approvals" className="text-lg font-medium text-foreground">
          Pending leave approvals
        </h2>
        {items.length > 0 ? <span className="text-sm text-muted-foreground">{items.length} awaiting review</span> : null}
      </div>

      {isLoading ? (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Spinner className="size-4" /> Loading leave requests…
        </div>
      ) : isError ? (
        <p className="mt-3 text-sm text-destructive">{getErrorMessage(error)}</p>
      ) : items.length === 0 ? (
        <p className="mt-3 text-sm text-muted-foreground">No leave requests are waiting for your review.</p>
      ) : (
        <div className="mt-3 overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="text-left text-muted-foreground">
                <th className="pb-2 pr-3">Employee</th>
                <th className="pb-2 pr-3">Leave type</th>
                <th className="pb-2 pr-3">Dates</th>
                <th className="pb-2 pr-3">Requested</th>
                <th className="pb-2 pr-3">Available</th>
                <th className="pb-2 pr-3">Reason</th>
                <th className="pb-2 pr-3">Status</th>
                <th className="pb-2 pr-3">Action</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.leaveRequestId} className="border-t border-border align-top">
                  <td className="py-2 pr-3">
                    <div className="font-medium text-foreground">{item.employeeName}</div>
                    <div className="text-xs text-muted-foreground">{item.employeeNumber}</div>
                  </td>
                  <td className="py-2 pr-3">{item.leaveTypeName}</td>
                  <td className="py-2 pr-3 whitespace-nowrap">
                    {item.startDateUtc.slice(0, 10)} to {item.endDateUtc.slice(0, 10)}
                  </td>
                  <td className="py-2 pr-3">
                    {item.requestedAmount} {unitLabel(item.unit)}
                  </td>
                  <td className="py-2 pr-3">
                    {item.availableBalance == null ? "—" : `${item.availableBalance} ${unitLabel(item.unit)}`}
                  </td>
                  <td className="py-2 pr-3 max-w-xs break-words">{item.reason || "—"}</td>
                  <td className="py-2 pr-3">{leaveRequestStatusLabel(item.status)}</td>
                  <td className="py-2 pr-3">
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        size="sm"
                        disabled={busy}
                        loading={approve.isPending && approve.variables?.id === item.leaveRequestId}
                        onClick={() => void handleApprove(item)}
                      >
                        Approve
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="danger"
                        disabled={busy}
                        onClick={() => openReject(item)}
                      >
                        Reject
                      </Button>
                    </div>
                    {rowErrors[item.leaveRequestId] ? (
                      <p className="mt-1 text-xs text-destructive" role="alert">
                        {rowErrors[item.leaveRequestId]}
                      </p>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Modal
        open={rejecting != null}
        title="Reject leave request"
        onClose={closeReject}
        footer={
          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={closeReject} disabled={reject.isPending}>
              Cancel
            </Button>
            <Button type="button" variant="danger" loading={reject.isPending} onClick={() => void handleReject()}>
              Reject request
            </Button>
          </div>
        }
      >
        {rejecting ? (
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              {rejecting.employeeName} — {rejecting.leaveTypeName}, {rejecting.startDateUtc.slice(0, 10)} to{" "}
              {rejecting.endDateUtc.slice(0, 10)}
            </p>
            <label className="block text-sm font-medium text-foreground" htmlFor="leave-reject-reason">
              Reason (shown to the employee)
            </label>
            <textarea
              id="leave-reject-reason"
              className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20"
              rows={4}
              maxLength={MAX_REASON_LENGTH}
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
            />
            {rejectError ? (
              <p className="text-sm text-destructive" role="alert">
                {rejectError}
              </p>
            ) : null}
          </div>
        ) : null}
      </Modal>
    </section>
  );
}

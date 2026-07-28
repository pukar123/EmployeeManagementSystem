"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";
import type { EmployeeScheduledChange } from "../types/employee.types";

const changeTypeLabels: Record<number, string> = {
  1: "Termination",
  2: "Archive",
  4: "Status change",
  5: "Department transfer",
  6: "Position transfer",
  7: "Manager transfer",
};

const statusLabels: Record<number, string> = {
  1: "Pending",
  2: "Processing",
  3: "Applied",
  4: "Cancelled",
  5: "Failed",
};

type EmployeeUpcomingChangesProps = {
  employeeId: number;
  canManage: boolean;
};

export function EmployeeUpcomingChanges({ employeeId, canManage }: EmployeeUpcomingChangesProps) {
  const queryClient = useQueryClient();
  const { data, isLoading, isError, error } = useQuery({
    queryKey: employeeKeys.scheduledChanges(employeeId),
    queryFn: () => employeeService.listScheduledChanges(employeeId),
  });

  const cancelMut = useMutation({
    mutationFn: (changeId: number) => employeeService.cancelScheduledChange(employeeId, changeId),
    onSuccess: async () => {
      toast.success("Scheduled change cancelled.");
      await queryClient.invalidateQueries({ queryKey: employeeKeys.scheduledChanges(employeeId) });
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const upcoming = (data ?? []).filter((c) => c.status === 1 || c.status === 2 || c.status === 5);

  if (isLoading) {
    return (
      <div className="flex justify-center py-4">
        <Spinner />
      </div>
    );
  }

  if (isError) {
    return <p className="text-sm text-destructive">{getErrorMessage(error)}</p>;
  }

  if (upcoming.length === 0) {
    return <p className="text-sm text-muted-foreground">No upcoming scheduled changes.</p>;
  }

  return (
    <ul className="space-y-3">
      {upcoming.map((change) => (
        <ScheduledChangeRow
          key={change.id}
          change={change}
          canManage={canManage}
          onCancel={() => cancelMut.mutate(change.id)}
          cancelBusy={cancelMut.isPending}
        />
      ))}
    </ul>
  );
}

function ScheduledChangeRow({
  change,
  canManage,
  onCancel,
  cancelBusy,
}: {
  change: EmployeeScheduledChange;
  canManage: boolean;
  onCancel: () => void;
  cancelBusy: boolean;
}) {
  return (
    <li className="flex flex-wrap items-start justify-between gap-2 rounded-lg border border-border p-3 text-sm">
      <div>
        <p className="font-medium text-foreground">{changeTypeLabels[change.changeType] ?? `Type ${change.changeType}`}</p>
        <p className="text-muted-foreground">
          Effective {new Date(change.effectiveAtUtc).toLocaleString()} · {statusLabels[change.status] ?? change.status}
        </p>
        {change.reason ? <p className="text-muted-foreground">Reason: {change.reason}</p> : null}
        {change.failureReason ? <p className="text-destructive">Failure: {change.failureReason}</p> : null}
      </div>
      {canManage && change.status === 1 ? (
        <Button type="button" variant="secondary" size="sm" disabled={cancelBusy} onClick={onCancel}>
          Cancel
        </Button>
      ) : null}
    </li>
  );
}

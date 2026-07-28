"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";

const deliveryLabels: Record<number, string> = {
  1: "Pending",
  2: "Sent",
  3: "Failed",
};

type EmployeeInvitationPanelProps = {
  employeeId: number;
  hasLinkedLogin: boolean;
  linkedLoginIsActive: boolean | null;
};

export function EmployeeInvitationPanel({
  employeeId,
  hasLinkedLogin,
  linkedLoginIsActive,
}: EmployeeInvitationPanelProps) {
  const queryClient = useQueryClient();
  const { data, isLoading, isError, error } = useQuery({
    queryKey: employeeKeys.invitations(employeeId),
    queryFn: () => employeeService.listInvitations(employeeId),
    enabled: !hasLinkedLogin || linkedLoginIsActive === false,
  });

  const sendMut = useMutation({
    mutationFn: () => employeeService.sendInvitation(employeeId),
    onSuccess: async () => {
      toast.success("Invitation email sent.");
      await queryClient.invalidateQueries({ queryKey: employeeKeys.invitations(employeeId) });
      await queryClient.invalidateQueries({ queryKey: employeeKeys.profile(employeeId) });
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

  const revokeMut = useMutation({
    mutationFn: (invitationId: number) => employeeService.revokeInvitation(employeeId, invitationId),
    onSuccess: async () => {
      toast.success("Invitation revoked.");
      await queryClient.invalidateQueries({ queryKey: employeeKeys.invitations(employeeId) });
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });

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

  const active = (data ?? []).find((i) => !i.usedAtUtc && !i.revokedAtUtc);

  return (
    <div className="space-y-3 rounded-lg border border-border p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-semibold text-foreground">Login invitation</h3>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          disabled={sendMut.isPending || (active != null && !active.canResend)}
          onClick={() => sendMut.mutate()}
        >
          {active ? "Resend invitation" : "Send invitation"}
        </Button>
      </div>
      {active ? (
        <dl className="grid gap-2 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">Status</dt>
            <dd>{deliveryLabels[active.deliveryStatus] ?? active.deliveryStatus}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Expires</dt>
            <dd>{new Date(active.expiresAtUtc).toLocaleString()}</dd>
          </div>
          {active.deliveryFailureReason ? (
            <div className="sm:col-span-2">
              <dt className="text-muted-foreground">Delivery error</dt>
              <dd className="text-destructive">{active.deliveryFailureReason}</dd>
            </div>
          ) : null}
          {active.resendCooldownSecondsRemaining > 0 ? (
            <div className="sm:col-span-2 text-muted-foreground">
              Resend available in {active.resendCooldownSecondsRemaining}s
            </div>
          ) : null}
        </dl>
      ) : (
        <p className="text-sm text-muted-foreground">No active invitation.</p>
      )}
      {active && active.canResend ? (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={revokeMut.isPending}
          onClick={() => revokeMut.mutate(active.id)}
        >
          Revoke invitation
        </Button>
      ) : null}
    </div>
  );
}

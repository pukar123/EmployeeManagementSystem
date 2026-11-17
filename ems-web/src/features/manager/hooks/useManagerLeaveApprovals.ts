import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { leaveService } from "@/features/leave/services/leaveService";
import { leaveKeys } from "@/features/leave/services/query-keys";
import { managerTeamService } from "../services/managerTeamService";
import { managerTeamKeys } from "../services/query-keys";
import type { ManagerLeaveApprovalQuery } from "../types/manager-team.types";

/** Manager's pending leave approval queue plus approve/reject actions. */
export function useManagerLeaveApprovals(query: ManagerLeaveApprovalQuery | null) {
  const queryClient = useQueryClient();

  const approvalsQuery = useQuery({
    queryKey: managerTeamKeys.leaveApprovals(query),
    queryFn: () => managerTeamService.getLeaveApprovals(query!),
    enabled: query != null && query.organizationId > 0,
    retry: (failureCount, error) => {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 403 || status === 401) return false;
      return failureCount < 2;
    },
  });

  // Refresh the queue, the team summary (pending count) and any cached leave data.
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: managerTeamKeys.all });
    void queryClient.invalidateQueries({ queryKey: leaveKeys.all });
  };

  const approve = useMutation({
    mutationFn: ({ id, comment }: { id: number; comment?: string }) =>
      leaveService.approveLeaveRequest(id, { comment: comment?.trim() || undefined }),
    onSuccess: refresh,
  });

  const reject = useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) =>
      leaveService.rejectLeaveRequest(id, { reason }),
    onSuccess: refresh,
  });

  return { approvalsQuery, approve, reject };
}

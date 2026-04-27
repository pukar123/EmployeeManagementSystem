import { useMutation, useQueryClient } from "@tanstack/react-query";
import { leaveService } from "../services/leaveService";
import { leaveKeys } from "../services/query-keys";
import type { CreateLeaveRequestPayload, UpdateLeaveRequestPayload } from "../types/leave.types";

export function useLeaveMutations(employeeId: number | null) {
  const queryClient = useQueryClient();
  const refresh = () => {
    if (employeeId == null) return;
    void queryClient.invalidateQueries({ queryKey: leaveKeys.requests(employeeId) });
    void queryClient.invalidateQueries({ queryKey: leaveKeys.balances(employeeId) });
  };

  const createRequest = useMutation({
    mutationFn: (payload: CreateLeaveRequestPayload) => leaveService.createLeaveRequest(payload),
    onSuccess: refresh,
  });

  const updateRequest = useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateLeaveRequestPayload }) =>
      leaveService.updateLeaveRequest(id, payload),
    onSuccess: refresh,
  });

  const cancelRequest = useMutation({
    mutationFn: (id: number) => leaveService.cancelLeaveRequest(id),
    onSuccess: refresh,
  });

  const uploadAttachment = useMutation({
    mutationFn: ({ leaveRequestId, file }: { leaveRequestId: number; file: File }) =>
      leaveService.uploadAttachment(leaveRequestId, file),
  });

  const bulkImport = useMutation({
    mutationFn: leaveService.bulkImport,
  });

  return { createRequest, updateRequest, cancelRequest, uploadAttachment, bulkImport };
}

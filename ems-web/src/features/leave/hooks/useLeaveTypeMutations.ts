import { useMutation, useQueryClient } from "@tanstack/react-query";
import { leaveService } from "../services/leaveService";
import { leaveKeys } from "../services/query-keys";
import type { CreateLeaveTypePayload, UpdateLeaveTypePayload } from "../types/leave.types";

export function useLeaveTypeMutations(organizationId: number | null) {
  const queryClient = useQueryClient();

  const refreshTypes = () => {
    if (organizationId == null) return;
    void queryClient.invalidateQueries({ queryKey: leaveKeys.types(organizationId) });
  };

  const createLeaveType = useMutation({
    mutationFn: (payload: CreateLeaveTypePayload) => leaveService.createLeaveType(payload),
    onSuccess: refreshTypes,
  });

  const updateLeaveType = useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateLeaveTypePayload }) =>
      leaveService.updateLeaveType(id, payload),
    onSuccess: refreshTypes,
  });

  const deleteLeaveType = useMutation({
    mutationFn: (id: number) => leaveService.deleteLeaveType(id),
    onSuccess: refreshTypes,
  });

  return { createLeaveType, updateLeaveType, deleteLeaveType };
}

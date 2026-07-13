import { useMutation, useQueryClient } from "@tanstack/react-query";
import { employeePortalKeys } from "@/features/employee-portal/services/query-keys";
import { shiftKeys } from "../services/query-keys";
import { shiftService } from "../services/shiftService";
import type { UpdateShiftRequest } from "../types/shift.types";

export function useUpdateShift() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateShiftRequest }) => shiftService.updateShift(id, body),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: shiftKeys.all }),
        queryClient.invalidateQueries({ queryKey: employeePortalKeys.all }),
      ]);
    },
  });
}

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { employeePortalKeys } from "@/features/employee-portal/services/query-keys";
import { shiftKeys } from "../services/query-keys";
import { shiftService } from "../services/shiftService";

export function useDeleteShift() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: shiftService.deleteShift,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: shiftKeys.all }),
        queryClient.invalidateQueries({ queryKey: employeePortalKeys.all }),
      ]);
    },
  });
}

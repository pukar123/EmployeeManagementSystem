import { useMutation, useQueryClient } from "@tanstack/react-query";
import { employeePortalKeys } from "../services/query-keys";
import { employeePortalService } from "../services/employeePortalService";

export function useStartShift() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (shiftId: number) => employeePortalService.startShift(shiftId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeePortalKeys.summary() });
    },
  });
}

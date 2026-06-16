import { useMutation, useQueryClient } from "@tanstack/react-query";
import { employeePortalKeys } from "../services/query-keys";
import { employeePortalService } from "../services/employeePortalService";

export function useStartTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (taskId: number) => employeePortalService.startTask(taskId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: employeePortalKeys.summary() });
    },
  });
}

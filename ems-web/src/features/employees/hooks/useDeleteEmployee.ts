import { useMutation, useQueryClient } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";

export function useDeleteEmployee() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => employeeService.archiveEmployee(id),
    onSuccess: (_void, id) => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.list() });
      void queryClient.removeQueries({ queryKey: employeeKeys.detail(id) });
    },
  });
}

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";

export function useSetEmployeeDirectRoles() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ employeeId, roleIds }: { employeeId: number; roleIds: number[] }) =>
      employeeService.setEmployeeDirectRoles(employeeId, { roleIds }),
    onSuccess: (_, variables) => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.effectiveRoles(variables.employeeId) });
      void queryClient.invalidateQueries({ queryKey: employeeKeys.list() });
    },
  });
}

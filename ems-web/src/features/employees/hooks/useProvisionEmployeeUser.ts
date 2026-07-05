import { useMutation, useQueryClient } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";

/** Sends an invitation via EMS → User Management (replaces legacy provision-user). */
export function useProvisionEmployeeUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (employeeId: number) => employeeService.sendInvitation(employeeId),
    onSuccess: (_, employeeId) => {
      void queryClient.invalidateQueries({ queryKey: employeeKeys.detail(employeeId) });
      void queryClient.invalidateQueries({ queryKey: employeeKeys.list() });
    },
  });
}

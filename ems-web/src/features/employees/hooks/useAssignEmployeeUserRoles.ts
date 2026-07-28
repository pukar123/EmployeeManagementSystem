import { useMutation } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";

export function useAssignEmployeeUserRoles() {
  return useMutation({
    mutationFn: ({ employeeId, roleKeys }: { employeeId: number; roleKeys: string[] }) =>
      employeeService.assignEmployeeUserRoles(employeeId, { roleKeys }),
  });
}

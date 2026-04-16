import { useMutation } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";

export function useAssignEmployeeUserRoles() {
  return useMutation({
    mutationFn: ({ employeeId, roleIds }: { employeeId: number; roleIds: number[] }) =>
      employeeService.assignEmployeeUserRoles(employeeId, { roleIds }),
  });
}

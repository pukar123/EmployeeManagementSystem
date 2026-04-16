import { useMutation } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";

export function useProvisionEmployeeUser() {
  return useMutation({
    mutationFn: (employeeId: number) => employeeService.provisionEmployeeUser(employeeId),
  });
}

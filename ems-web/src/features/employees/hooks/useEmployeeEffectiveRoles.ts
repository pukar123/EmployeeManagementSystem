import { useQuery } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";

export function useEmployeeEffectiveRoles(employeeId: number | null) {
  return useQuery({
    queryKey: employeeId == null ? employeeKeys.all : employeeKeys.effectiveRoles(employeeId),
    queryFn: () => employeeService.getEmployeeEffectiveRoles(employeeId!),
    enabled: employeeId != null,
  });
}

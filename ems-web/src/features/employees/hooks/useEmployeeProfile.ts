import { useQuery } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";

export function useEmployeeProfile(employeeId: number | null) {
  return useQuery({
    queryKey: employeeKeys.profile(employeeId ?? 0),
    queryFn: () => employeeService.getEmployeeProfile(employeeId!),
    enabled: employeeId != null && employeeId > 0,
  });
}

export function useEmployeeHistory(employeeId: number | null) {
  return useQuery({
    queryKey: employeeKeys.history(employeeId ?? 0),
    queryFn: () => employeeService.getEmployeeHistory(employeeId!),
    enabled: employeeId != null && employeeId > 0,
  });
}

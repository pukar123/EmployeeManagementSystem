import { useQuery } from "@tanstack/react-query";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";
import type { EmployeeDirectoryQuery } from "../types/employee.types";

export function useEmployeeDirectory(query: EmployeeDirectoryQuery | null) {
  return useQuery({
    queryKey: employeeKeys.directory(query ?? {}),
    queryFn: () => employeeService.getEmployeeDirectory(query!),
    enabled: query != null && query.organizationId > 0,
    placeholderData: (prev) => prev,
  });
}

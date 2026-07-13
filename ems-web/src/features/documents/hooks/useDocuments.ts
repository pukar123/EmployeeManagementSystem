import { useQuery } from "@tanstack/react-query";
import { documentService } from "../services/documentService";
import { documentKeys } from "../services/query-keys";

export function useDocuments(employeeId: number, enabled = true) {
  return useQuery({
    queryKey: documentKeys.byEmployee(employeeId),
    queryFn: () => documentService.listByEmployee(employeeId),
    enabled: employeeId > 0 && enabled,
  });
}

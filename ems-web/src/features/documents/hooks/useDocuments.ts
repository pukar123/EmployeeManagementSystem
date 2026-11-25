import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { documentService } from "../services/documentService";
import { documentKeys } from "../services/query-keys";
import type { DocumentListParams } from "../types/document.types";
import { isForbiddenError } from "../utils/document-paging";

/** One page of an employee's documents. The server decides who may see them (B09). */
export function useDocuments(employeeId: number, params: DocumentListParams, enabled = true) {
  return useQuery({
    queryKey: documentKeys.byEmployeePage(employeeId, params),
    queryFn: () => documentService.listByEmployee(employeeId, params),
    enabled: employeeId > 0 && enabled,
    placeholderData: keepPreviousData,
    retry: (failureCount, error) => !isForbiddenError(error) && failureCount < 2,
  });
}

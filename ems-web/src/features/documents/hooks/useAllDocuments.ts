import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { documentService } from "../services/documentService";
import { documentKeys } from "../services/query-keys";
import type { DocumentListParams } from "../types/document.types";
import { isForbiddenError } from "../utils/document-paging";

/** One page of every document. Document admins only; enable it only when `canManageAllDocuments` is true. */
export function useAllDocuments(params: DocumentListParams, enabled = true) {
  return useQuery({
    queryKey: documentKeys.allListPage(params),
    queryFn: () => documentService.list(params),
    enabled,
    placeholderData: keepPreviousData,
    retry: (failureCount, error) => !isForbiddenError(error) && failureCount < 2,
  });
}

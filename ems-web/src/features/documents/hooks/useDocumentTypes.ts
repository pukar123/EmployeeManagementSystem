import { useQuery } from "@tanstack/react-query";
import { documentService } from "../services/documentService";
import { documentKeys } from "../services/query-keys";

export function useDocumentTypes(enabled = true) {
  return useQuery({
    queryKey: documentKeys.types(),
    queryFn: () => documentService.getTypes(),
    enabled,
    staleTime: 5 * 60_000,
  });
}

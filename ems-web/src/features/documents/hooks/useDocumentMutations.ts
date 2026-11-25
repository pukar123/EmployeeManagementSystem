import { useMutation, useQueryClient } from "@tanstack/react-query";
import { documentService } from "../services/documentService";
import { documentKeys } from "../services/query-keys";
import type { CreateDocumentPayload, Document, UpdateDocumentRequest } from "../types/document.types";

export function useDocumentMutations(employeeId: number) {
  const queryClient = useQueryClient();

  // Prefix keys, so every page of this employee's documents and of the all-documents list refreshes.
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: documentKeys.byEmployee(employeeId) });
    void queryClient.invalidateQueries({ queryKey: documentKeys.allList() });
  };

  const create = useMutation({
    mutationFn: (payload: CreateDocumentPayload) => documentService.create(payload),
    onSuccess: refresh,
  });

  const update = useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateDocumentRequest }) =>
      documentService.update(id, body),
    onSuccess: refresh,
  });

  const remove = useMutation({
    mutationFn: (id: number) => documentService.delete(id),
    onSuccess: refresh,
  });

  const download = useMutation({
    mutationFn: (doc: Pick<Document, "downloadUrl" | "originalFileName">) => documentService.download(doc),
  });

  return { create, update, remove, download };
}

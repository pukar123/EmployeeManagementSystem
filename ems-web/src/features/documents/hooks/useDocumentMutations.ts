import { useMutation, useQueryClient } from "@tanstack/react-query";
import { documentService } from "../services/documentService";
import { documentKeys } from "../services/query-keys";
import type { CreateDocumentPayload, UpdateDocumentRequest } from "../types/document.types";

export function useDocumentMutations(employeeId: number) {
  const queryClient = useQueryClient();

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: documentKeys.byEmployee(employeeId) });
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
    mutationFn: ({ id, fileName }: { id: number; fileName: string }) =>
      documentService.download(id, fileName),
  });

  return { create, update, remove, download };
}

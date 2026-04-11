import { useMutation, useQueryClient } from "@tanstack/react-query";
import { siteService } from "../services/siteService";
import { siteKeys } from "../services/query-keys";

export function useDeleteSite() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => siteService.deleteSite(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: siteKeys.list() });
    },
  });
}

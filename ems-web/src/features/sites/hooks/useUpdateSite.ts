import { useMutation, useQueryClient } from "@tanstack/react-query";
import { siteService } from "../services/siteService";
import { siteKeys } from "../services/query-keys";
import type { UpdateSiteRequest } from "../types/site.types";

export function useUpdateSite() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateSiteRequest }) =>
      siteService.updateSite(id, body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: siteKeys.list() });
    },
  });
}

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { siteService } from "../services/siteService";
import { siteKeys } from "../services/query-keys";
import type { CreateSiteRequest } from "../types/site.types";

export function useCreateSite() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateSiteRequest) => siteService.createSite(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: siteKeys.list() });
    },
  });
}

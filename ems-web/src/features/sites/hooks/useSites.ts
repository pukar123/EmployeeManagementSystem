import { useQuery } from "@tanstack/react-query";
import { siteService } from "../services/siteService";
import { siteKeys } from "../services/query-keys";

export function useSites() {
  return useQuery({
    queryKey: siteKeys.list(),
    queryFn: () => siteService.getSites(),
  });
}

import { useQuery } from "@tanstack/react-query";
import { managerTeamService } from "../services/managerTeamService";
import { managerTeamKeys } from "../services/query-keys";
import type { ManagerTeamQuery } from "../types/manager-team.types";

export function useManagerTeam(query: ManagerTeamQuery | null) {
  return useQuery({
    queryKey: managerTeamKeys.dashboard(query),
    queryFn: () => managerTeamService.getDashboard(query!),
    enabled: query != null && query.organizationId > 0,
    retry: (failureCount, error) => {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 403 || status === 401) return false;
      return failureCount < 2;
    },
  });
}

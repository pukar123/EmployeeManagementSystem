import { emsHttpClient } from "@/shared/api/http-client";
import type { ManagerTeamDashboard, ManagerTeamQuery } from "../types/manager-team.types";

const PATH = "/api/Manager/team";

function toQueryParams(query: ManagerTeamQuery): Record<string, string | number> {
  const params: Record<string, string | number> = {
    organizationId: query.organizationId,
    page: query.page ?? 1,
    pageSize: query.pageSize ?? 25,
    sortBy: query.sortBy ?? "name",
    sortDirection: query.sortDirection ?? "asc",
  };

  if (query.managerId != null) params.managerId = query.managerId;
  if (query.search?.trim()) params.search = query.search.trim();
  if (query.employmentStatus != null) params.employmentStatus = query.employmentStatus;
  if (query.departmentId != null) params.departmentId = query.departmentId;

  return params;
}

export const managerTeamService = {
  getDashboard: async (query: ManagerTeamQuery): Promise<ManagerTeamDashboard> => {
    const { data } = await emsHttpClient.get<ManagerTeamDashboard>(PATH, {
      params: toQueryParams(query),
    });
    return data;
  },
};

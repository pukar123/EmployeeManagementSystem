export const managerTeamKeys = {
  all: ["manager-team"] as const,
  dashboard: (query: unknown) => [...managerTeamKeys.all, "dashboard", query] as const,
  leaveApprovals: (query: unknown) => [...managerTeamKeys.all, "leave-approvals", query] as const,
};

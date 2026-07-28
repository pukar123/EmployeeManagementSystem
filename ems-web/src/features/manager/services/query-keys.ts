export const managerTeamKeys = {
  all: ["manager-team"] as const,
  dashboard: (query: unknown) => [...managerTeamKeys.all, "dashboard", query] as const,
};

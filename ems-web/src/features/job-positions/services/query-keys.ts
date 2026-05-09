export const jobPositionKeys = {
  all: ["jobPositions"] as const,
  list: (organizationId: number) => [...jobPositionKeys.all, "list", organizationId] as const,
  roles: (jobPositionId: number) => [...jobPositionKeys.all, "roles", jobPositionId] as const,
};

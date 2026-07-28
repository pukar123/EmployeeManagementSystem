export const employeeKeys = {
  all: ["employees"] as const,
  lists: () => [...employeeKeys.all, "list"] as const,
  list: () => employeeKeys.lists(),
  directory: (query: Record<string, unknown>) => [...employeeKeys.all, "directory", query] as const,
  details: () => [...employeeKeys.all, "detail"] as const,
  detail: (id: number) => [...employeeKeys.details(), id] as const,
  profile: (id: number) => [...employeeKeys.all, "profile", id] as const,
  history: (id: number) => [...employeeKeys.all, "history", id] as const,
  effectiveRoles: (id: number) => [...employeeKeys.all, "effective-roles", id] as const,
  scheduledChanges: (id: number) => [...employeeKeys.all, "scheduled-changes", id] as const,
  invitations: (id: number) => [...employeeKeys.all, "invitations", id] as const,
};

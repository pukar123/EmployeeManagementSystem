export const employeeKeys = {
  all: ["employees"] as const,
  lists: () => [...employeeKeys.all, "list"] as const,
  list: () => employeeKeys.lists(),
  details: () => [...employeeKeys.all, "detail"] as const,
  detail: (id: number) => [...employeeKeys.details(), id] as const,
  effectiveRoles: (id: number) => [...employeeKeys.all, "effective-roles", id] as const,
};

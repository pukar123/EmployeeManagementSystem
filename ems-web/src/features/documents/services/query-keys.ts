export const documentKeys = {
  all: ["documents"] as const,
  types: () => [...documentKeys.all, "types"] as const,
  byEmployee: (employeeId: number) => [...documentKeys.all, "employee", employeeId] as const,
  detail: (id: number) => [...documentKeys.all, "detail", id] as const,
};

export const taskKeys = {
  all: ["tasks"] as const,
  list: (employeeId?: number | null) => [...taskKeys.all, "list", employeeId ?? "all"] as const,
  detail: (id: number) => [...taskKeys.all, "detail", id] as const,
};

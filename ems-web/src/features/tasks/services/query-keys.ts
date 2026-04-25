import type { TaskQueryParams } from "../types/task.types";

export const taskKeys = {
  all: ["tasks"] as const,
  list: (query: TaskQueryParams = {}) =>
    [
      ...taskKeys.all,
      "list",
      query.employeeId ?? "all",
      query.assignedByUserId ?? "all",
      query.rangeStartUtc ?? "none",
      query.rangeEndUtc ?? "none",
    ] as const,
  detail: (id: number) => [...taskKeys.all, "detail", id] as const,
};

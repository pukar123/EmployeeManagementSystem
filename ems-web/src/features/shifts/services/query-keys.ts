import type { ShiftQueryParams } from "../types/shift.types";

export const shiftKeys = {
  all: ["shifts"] as const,
  list: (query: ShiftQueryParams = {}) =>
    [
      ...shiftKeys.all,
      "list",
      query.organizationId ?? "all",
      query.employeeId ?? "all",
    ] as const,
  detail: (id: number) => [...shiftKeys.all, "detail", id] as const,
};

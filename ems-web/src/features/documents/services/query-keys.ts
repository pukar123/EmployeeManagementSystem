import type { DocumentListParams } from "../types/document.types";

export const documentKeys = {
  all: ["documents"] as const,
  types: () => [...documentKeys.all, "types"] as const,
  /** Prefix for every page of one employee's documents; invalidate this to refresh them all. */
  byEmployee: (employeeId: number) => [...documentKeys.all, "employee", employeeId] as const,
  byEmployeePage: (employeeId: number, params: DocumentListParams) =>
    [...documentKeys.byEmployee(employeeId), params] as const,
  /** Prefix for every page of the all-documents list (document admins). */
  allList: () => [...documentKeys.all, "list"] as const,
  allListPage: (params: DocumentListParams) => [...documentKeys.allList(), params] as const,
  detail: (id: number) => [...documentKeys.all, "detail", id] as const,
};

export const attendanceKeys = {
  all: ["attendance"] as const,
  employee: (employeeId: number, fromDate?: string, toDate?: string) =>
    [...attendanceKeys.all, "employee", employeeId, fromDate ?? "", toDate ?? ""] as const,
  summary: (employeeId: number, fromDate: string, toDate: string) =>
    [...attendanceKeys.all, "summary", employeeId, fromDate, toDate] as const,
};

export const attendanceKeys = {
  all: ["attendance"] as const,
  employee: (employeeId: number, fromDate?: string, toDate?: string) =>
    [...attendanceKeys.all, "employee", employeeId, fromDate ?? "", toDate ?? ""] as const,
  summary: (employeeId: number, fromDate: string, toDate: string) =>
    [...attendanceKeys.all, "summary", employeeId, fromDate, toDate] as const,
  reportDaily: (organizationId: number, fromDate: string, toDate: string, employeeId?: number, departmentId?: number) =>
    [...attendanceKeys.all, "reportDaily", organizationId, fromDate, toDate, employeeId ?? 0, departmentId ?? 0] as const,
  reportPeriod: (
    organizationId: number,
    fromDate: string,
    toDate: string,
    groupBy: "week" | "month",
    employeeId?: number,
    departmentId?: number,
  ) => [...attendanceKeys.all, "reportPeriod", organizationId, fromDate, toDate, groupBy, employeeId ?? 0, departmentId ?? 0] as const,
  punctuality: (organizationId: number, fromDate: string, toDate: string, employeeId?: number, departmentId?: number) =>
    [...attendanceKeys.all, "punctuality", organizationId, fromDate, toDate, employeeId ?? 0, departmentId ?? 0] as const,
  absenteeism: (organizationId: number, fromDate: string, toDate: string, employeeId?: number, departmentId?: number) =>
    [...attendanceKeys.all, "absenteeism", organizationId, fromDate, toDate, employeeId ?? 0, departmentId ?? 0] as const,
};

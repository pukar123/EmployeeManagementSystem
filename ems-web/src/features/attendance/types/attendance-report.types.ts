export type AttendanceReportFilter = {
  organizationId: number;
  fromDate: string;
  toDate: string;
  employeeId?: number;
  departmentId?: number;
  groupBy?: "week" | "month";
};

export type AttendanceDailySummary = {
  workDate: string;
  organizationId: number;
  employeeId: number | null;
  departmentId: number | null;
  totalRecords: number;
  presentEmployees: number;
  totalWorkedMinutes: number;
  totalBreakMinutes: number;
  averageWorkedMinutes: number;
};

export type AttendancePeriodSummary = {
  periodLabel: string;
  periodStartDate: string;
  periodEndDate: string;
  organizationId: number;
  employeeId: number | null;
  departmentId: number | null;
  totalRecords: number;
  presentEmployees: number;
  totalWorkedMinutes: number;
  totalBreakMinutes: number;
  averageWorkedMinutes: number;
};

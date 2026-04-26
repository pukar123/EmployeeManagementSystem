import type { AttendanceReportFilter } from "./attendance-report.types";

export type AttendancePunctualityAnalytics = {
  organizationId: number;
  fromDate: string;
  toDate: string;
  employeeId: number | null;
  departmentId: number | null;
  totalRecords: number;
  lateArrivals: number;
  earlyDepartures: number;
  lateArrivalRate: number;
  earlyDepartureRate: number;
};

export type AttendanceAbsenteeismAnalytics = {
  organizationId: number;
  fromDate: string;
  toDate: string;
  employeeId: number | null;
  departmentId: number | null;
  expectedWorkDays: number;
  presentDays: number;
  absentDays: number;
  absenceRate: number;
};

export type AttendanceExportFormat = "csv" | "xlsx" | "pdf";

export type AttendanceExportRequest = AttendanceReportFilter & {
  format: AttendanceExportFormat;
};

import { httpClient } from "@/shared/api/http-client";
import type { AttendanceAbsenteeismAnalytics, AttendancePunctualityAnalytics } from "../types/attendance-analytics.types";
import type { AttendanceDailySummary, AttendancePeriodSummary, AttendanceReportFilter } from "../types/attendance-report.types";

const PATH = "/api/Attendance";

export const attendanceReportingService = {
  getDailySummaries: async (filter: AttendanceReportFilter): Promise<AttendanceDailySummary[]> => {
    const { data } = await httpClient.get<AttendanceDailySummary[]>(`${PATH}/reports/daily`, { params: filter });
    return data;
  },

  getPeriodSummaries: async (filter: AttendanceReportFilter): Promise<AttendancePeriodSummary[]> => {
    const { data } = await httpClient.get<AttendancePeriodSummary[]>(`${PATH}/reports/period`, { params: filter });
    return data;
  },

  getPunctuality: async (filter: AttendanceReportFilter): Promise<AttendancePunctualityAnalytics> => {
    const { data } = await httpClient.get<AttendancePunctualityAnalytics>(`${PATH}/analytics/punctuality`, { params: filter });
    return data;
  },

  getAbsenteeism: async (filter: AttendanceReportFilter): Promise<AttendanceAbsenteeismAnalytics> => {
    const { data } = await httpClient.get<AttendanceAbsenteeismAnalytics>(`${PATH}/analytics/absenteeism`, { params: filter });
    return data;
  },
};

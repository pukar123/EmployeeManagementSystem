import { useQuery } from "@tanstack/react-query";
import { attendanceKeys } from "../services/query-keys";
import { attendanceReportingService } from "../services/attendanceReportingService";
import type { AttendanceReportFilter } from "../types/attendance-report.types";

export function useAttendanceDailyReport(filter: AttendanceReportFilter | null) {
  return useQuery({
    queryKey: attendanceKeys.reportDaily(
      filter?.organizationId ?? 0,
      filter?.fromDate ?? "",
      filter?.toDate ?? "",
      filter?.employeeId,
      filter?.departmentId,
    ),
    queryFn: () => attendanceReportingService.getDailySummaries(filter!),
    enabled: filter != null,
  });
}

export function useAttendancePeriodReport(filter: AttendanceReportFilter | null) {
  return useQuery({
    queryKey: attendanceKeys.reportPeriod(
      filter?.organizationId ?? 0,
      filter?.fromDate ?? "",
      filter?.toDate ?? "",
      filter?.groupBy ?? "week",
      filter?.employeeId,
      filter?.departmentId,
    ),
    queryFn: () => attendanceReportingService.getPeriodSummaries(filter!),
    enabled: filter != null,
  });
}

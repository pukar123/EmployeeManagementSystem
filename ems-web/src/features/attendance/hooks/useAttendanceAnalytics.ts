import { useQuery } from "@tanstack/react-query";
import { attendanceKeys } from "../services/query-keys";
import { attendanceReportingService } from "../services/attendanceReportingService";
import type { AttendanceReportFilter } from "../types/attendance-report.types";

export function useAttendancePunctuality(filter: AttendanceReportFilter | null) {
  return useQuery({
    queryKey: attendanceKeys.punctuality(
      filter?.organizationId ?? 0,
      filter?.fromDate ?? "",
      filter?.toDate ?? "",
      filter?.employeeId,
      filter?.departmentId,
    ),
    queryFn: () => attendanceReportingService.getPunctuality(filter!),
    enabled: filter != null,
  });
}

export function useAttendanceAbsenteeism(filter: AttendanceReportFilter | null) {
  return useQuery({
    queryKey: attendanceKeys.absenteeism(
      filter?.organizationId ?? 0,
      filter?.fromDate ?? "",
      filter?.toDate ?? "",
      filter?.employeeId,
      filter?.departmentId,
    ),
    queryFn: () => attendanceReportingService.getAbsenteeism(filter!),
    enabled: filter != null,
  });
}

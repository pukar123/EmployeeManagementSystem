import { useQuery } from "@tanstack/react-query";
import { attendanceKeys } from "../services/query-keys";
import { attendanceService } from "../services/attendanceService";

export function useAttendanceSummary(employeeId: number | null, fromDate: string, toDate: string) {
  return useQuery({
    queryKey: attendanceKeys.summary(employeeId ?? 0, fromDate, toDate),
    queryFn: () => attendanceService.getSummary(employeeId!, fromDate, toDate),
    enabled: employeeId != null,
  });
}

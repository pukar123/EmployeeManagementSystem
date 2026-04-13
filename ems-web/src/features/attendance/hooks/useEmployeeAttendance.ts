import { useQuery } from "@tanstack/react-query";
import { attendanceKeys } from "../services/query-keys";
import { attendanceService } from "../services/attendanceService";

export function useEmployeeAttendance(employeeId: number | null, fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: attendanceKeys.employee(employeeId ?? 0, fromDate, toDate),
    queryFn: () => attendanceService.getEmployeeAttendance(employeeId!, fromDate, toDate),
    enabled: employeeId != null,
  });
}

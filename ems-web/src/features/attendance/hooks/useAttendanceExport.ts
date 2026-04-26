import { useMutation } from "@tanstack/react-query";
import { exportAttendanceReport } from "../services/attendanceExportService";
import type { AttendanceExportRequest } from "../types/attendance-analytics.types";

export function useAttendanceExport() {
  return useMutation({
    mutationFn: (request: AttendanceExportRequest) => exportAttendanceReport(request),
  });
}

import { httpClient } from "@/shared/api/http-client";
import type { AttendanceExportRequest } from "../types/attendance-analytics.types";

const PATH = "/api/Attendance";

export async function exportAttendanceReport(request: AttendanceExportRequest): Promise<Blob> {
  const { data } = await httpClient.get(`${PATH}/reports/export`, {
    params: request,
    responseType: "blob",
  });
  return data as Blob;
}

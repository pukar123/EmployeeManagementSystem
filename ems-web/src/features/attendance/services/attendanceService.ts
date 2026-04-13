import { httpClient } from "@/shared/api/http-client";
import type {
  AttendanceBreak,
  AttendanceRecord,
  AttendanceSummary,
  CheckInRequest,
  CheckOutRequest,
  EndBreakRequest,
  ManualAttendanceEntryRequest,
  StartBreakRequest,
} from "../types/attendance.types";

const PATH = "/api/Attendance";

export const attendanceService = {
  getEmployeeAttendance: async (
    employeeId: number,
    fromDate?: string,
    toDate?: string,
  ): Promise<AttendanceRecord[]> => {
    const { data } = await httpClient.get<AttendanceRecord[]>(`${PATH}/employee/${employeeId}`, {
      params: { fromDate, toDate },
    });
    return data;
  },

  getSummary: async (employeeId: number, fromDate: string, toDate: string): Promise<AttendanceSummary> => {
    const { data } = await httpClient.get<AttendanceSummary>(`${PATH}/summary`, {
      params: { employeeId, fromDate, toDate },
    });
    return data;
  },

  checkIn: async (body: CheckInRequest): Promise<AttendanceRecord> => {
    const { data } = await httpClient.post<AttendanceRecord>(`${PATH}/check-in`, body);
    return data;
  },

  checkOut: async (body: CheckOutRequest): Promise<AttendanceRecord> => {
    const { data } = await httpClient.post<AttendanceRecord>(`${PATH}/check-out`, body);
    return data;
  },

  startBreak: async (body: StartBreakRequest): Promise<AttendanceBreak> => {
    const { data } = await httpClient.post<AttendanceBreak>(`${PATH}/break/start`, body);
    return data;
  },

  endBreak: async (body: EndBreakRequest): Promise<AttendanceBreak> => {
    const { data } = await httpClient.post<AttendanceBreak>(`${PATH}/break/end`, body);
    return data;
  },

  createManualEntry: async (body: ManualAttendanceEntryRequest): Promise<AttendanceRecord> => {
    const { data } = await httpClient.post<AttendanceRecord>(`${PATH}/manual-entry`, body);
    return data;
  },
};

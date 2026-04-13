export type AttendanceSource = 1 | 2;
export type AttendanceStatus = 1 | 2 | 3;

export type AttendanceBreak = {
  id: number;
  attendanceRecordId: number;
  startAtUtc: string;
  endAtUtc: string | null;
  durationMinutes: number | null;
};

export type AttendanceRecord = {
  id: number;
  organizationId: number;
  employeeId: number;
  workDate: string;
  checkInAtUtc: string;
  checkOutAtUtc: string | null;
  checkInLatitude: number | null;
  checkInLongitude: number | null;
  checkOutLatitude: number | null;
  checkOutLongitude: number | null;
  source: AttendanceSource;
  status: AttendanceStatus;
  manualReason: string | null;
  breakMinutes: number;
  workedMinutes: number;
  breaks: AttendanceBreak[];
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type AttendanceSummary = {
  employeeId: number;
  fromDate: string;
  toDate: string;
  totalRecords: number;
  totalBreakMinutes: number;
  totalWorkedMinutes: number;
};

export type CheckInRequest = {
  employeeId: number;
  checkInAtUtc?: string;
  latitude?: number;
  longitude?: number;
};

export type CheckOutRequest = {
  employeeId: number;
  checkOutAtUtc?: string;
  latitude?: number;
  longitude?: number;
};

export type StartBreakRequest = {
  employeeId: number;
  startAtUtc?: string;
};

export type EndBreakRequest = {
  employeeId: number;
  endAtUtc?: string;
};

export type ManualAttendanceEntryRequest = {
  organizationId: number;
  employeeId: number;
  workDate: string;
  checkInAtUtc: string;
  checkOutAtUtc: string;
  manualReason?: string | null;
};

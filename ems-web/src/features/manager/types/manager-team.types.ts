import type { EmploymentStatusValue } from "@/features/employees/types/employment-status";

export type ManagerTeamTodayAttendanceStatus = 0 | 1 | 2 | 3;

export const ManagerTeamTodayAttendanceStatus = {
  Present: 0,
  Absent: 1,
  OnLeave: 2,
  CheckedInOpen: 3,
} as const;

export type ManagerTeamQuery = {
  organizationId: number;
  managerId?: number | null;
  page?: number;
  pageSize?: number;
  search?: string | null;
  employmentStatus?: EmploymentStatusValue | null;
  departmentId?: number | null;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
};

export type ManagerTeamAttendanceTodaySummary = {
  workDateUtc: string;
  presentCount: number;
  absentCount: number;
  onLeaveCount: number;
  checkedInOpenCount: number;
};

export type ManagerTeamSummary = {
  activeEmployeeCount: number;
  pendingLeaveRequestCount: number;
  overdueTaskCount: number;
  upcomingScheduledChangeCount: number;
  attendanceToday: ManagerTeamAttendanceTodaySummary;
};

export type ManagerTeamMember = {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  employmentStatus: EmploymentStatusValue;
  isActive: boolean;
  isArchived: boolean;
  departmentId?: number | null;
  departmentName?: string | null;
  jobPositionId?: number | null;
  jobPositionTitle?: string | null;
  primarySiteName?: string | null;
  pendingLeaveCount: number;
  overdueTaskCount: number;
  upcomingScheduledChangeCount: number;
  todayAttendanceStatus: ManagerTeamTodayAttendanceStatus;
};

export type ManagerTeamDashboard = {
  managerId: number;
  managerName: string;
  managerEmployeeNumber: string;
  allowsManagerSelection: boolean;
  summary: ManagerTeamSummary;
  items: ManagerTeamMember[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export const defaultManagerTeamQuery = (organizationId: number): ManagerTeamQuery => ({
  organizationId,
  page: 1,
  pageSize: 25,
  sortBy: "name",
  sortDirection: "asc",
});

export const todayAttendanceStatusLabels: Record<ManagerTeamTodayAttendanceStatus, string> = {
  0: "Present",
  1: "Absent",
  2: "On leave",
  3: "Checked in",
};

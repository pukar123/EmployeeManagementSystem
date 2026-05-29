import type { LeaveBalance, LeaveRequest } from "@/features/leave/types/leave.types";

/** Matches `EMS.Domain.Enums.ShiftStatus` JSON (numeric). */
export type ShiftStatusValue = 0 | 1 | 2 | 3;

export type PortalShift = {
  id: number;
  organizationId: number;
  employeeId: number;
  siteId?: number | null;
  title: string;
  description?: string | null;
  startAtUtc: string;
  endAtUtc: string;
  status: ShiftStatusValue;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type EmployeePortalSummary = {
  employeeId: number;
  organizationId: number;
  nearestUpcomingShift: PortalShift | null;
  topThreeUpcomingShifts: PortalShift[];
  allUpcomingShifts: PortalShift[];
  leaveBalances: LeaveBalance[];
  leaveRequests: LeaveRequest[];
};

export type { LeaveBalance, LeaveRequest };

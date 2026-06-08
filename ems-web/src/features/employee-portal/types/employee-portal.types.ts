import type { LeaveBalance, LeaveRequest } from "@/features/leave/types/leave.types";

/** Matches `EMS.Domain.Enums.ShiftStatus` JSON (numeric). */
export type ShiftStatusValue = 0 | 1 | 2 | 3;

export type EmployeePortalEligibility = {
  hasLinkedEmployeeProfile: boolean;
  linkedEmployeeId: number | null;
  linkedOrganizationId: number | null;
  canManageOtherEmployeesLeave: boolean;
};

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
  hasLinkedEmployeeProfile: boolean;
  employeeId: number | null;
  organizationId: number | null;
  nearestUpcomingShift: PortalShift | null;
  topThreeUpcomingShifts: PortalShift[];
  allUpcomingShifts: PortalShift[];
  leaveBalances: LeaveBalance[];
  leaveRequests: LeaveRequest[];
};

export type { LeaveBalance, LeaveRequest };

import type { LeaveBalance, LeaveRequest } from "@/features/leave/types/leave.types";
import type { TaskItem } from "@/features/tasks/types/task.types";

/** Matches `EMS.Domain.Enums.ShiftStatus` JSON (numeric). */
export type ShiftStatusValue = 0 | 1 | 2 | 3;

/** Matches `EMS.Application.DTOs.EmployeePortal.PortalScheduleEntryKind` JSON. */
export type PortalScheduleEntryKind = 0 | 1;

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

export type PortalTask = TaskItem;

/** One row in the unified portal schedule (shift or task). */
export type PortalScheduleEntry = {
  kind: PortalScheduleEntryKind;
  shift: PortalShift | null;
  task: PortalTask | null;
};

export type EmployeePortalEligibility = {
  hasLinkedEmployeeProfile: boolean;
  linkedEmployeeId: number | null;
  linkedOrganizationId: number | null;
  canManageOtherEmployeesLeave: boolean;
  /** True for attendance admins (admin, or the attendance.admin menu): they may record and read anyone's attendance. */
  canManageOtherEmployeesAttendance: boolean;
  /** True for document admins (admin, or the documents.admin menu): they may list all documents and upload, edit and delete. */
  canManageAllDocuments: boolean;
};

export type EmployeePortalSummary = {
  hasLinkedEmployeeProfile: boolean;
  employeeId: number | null;
  organizationId: number | null;
  schedule: PortalScheduleEntry[];
  leaveBalances: LeaveBalance[];
  leaveRequests: LeaveRequest[];
};

export type { LeaveBalance, LeaveRequest };

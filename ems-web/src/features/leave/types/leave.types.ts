export type LeaveUnitValue = 1 | 2;

export type LeaveType = {
  id: number;
  organizationId: number;
  name: string;
  description?: string | null;
  unit: LeaveUnitValue;
  requiresAttachment: boolean;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type LeaveBalance = {
  id: number;
  organizationId: number;
  employeeId: number;
  leaveTypeId: number;
  openingBalance: number;
  accruedAmount: number;
  usedAmount: number;
  adjustedAmount: number;
  carryForwardAmount: number;
  availableAmount: number;
  balanceAsOfUtc: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type LeaveRequest = {
  id: number;
  organizationId: number;
  employeeId: number;
  leaveTypeId: number;
  startDateUtc: string;
  endDateUtc: string;
  unit: LeaveUnitValue;
  requestedAmount: number;
  reason?: string | null;
  status: string;
  submittedAtUtc: string;
  reviewedAtUtc?: string | null;
  reviewedByEmployeeId?: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type LeaveAdminSummary = {
  organizationId: number;
  asOfDateUtc: string;
  appliedCount: number;
  approvedCount: number;
  rejectedCount: number;
  cancelledCount: number;
  currentlyOnLeaveCount: number;
};

export type CreateLeaveRequestPayload = {
  employeeId: number;
  leaveTypeId: number;
  startDateUtc: string;
  endDateUtc: string;
  unit: LeaveUnitValue;
  requestedAmount: number;
  reason?: string;
};

export type UpdateLeaveRequestPayload = {
  startDateUtc: string;
  endDateUtc: string;
  unit: LeaveUnitValue;
  requestedAmount: number;
  reason?: string;
};

export type LeavePolicyRule = {
  id: number;
  organizationId: number;
  leaveTypeId: number;
  accrualRatePerPeriod: number;
  accrualFrequency: "Monthly" | "PayCycle" | "Yearly";
  maximumCarryForward?: number | null;
  enableProration: boolean;
  isActive: boolean;
  effectiveFromDateUtc: string;
  effectiveToDateUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type BulkLeaveImportItem = {
  employeeId: number;
  leaveTypeId: number;
  startDateUtc: string;
  endDateUtc: string;
  unit: LeaveUnitValue;
  requestedAmount: number;
  reason?: string;
};

export type BulkLeaveImportPayload = {
  organizationId: number;
  importKey?: string;
  items: BulkLeaveImportItem[];
};

export type BulkLeaveImportResult = {
  importKey?: string;
  totalRows: number;
  importedRows: number;
  failedRows: number;
  rowResults: Array<{ rowNumber: number; success: boolean; leaveRequestId?: number; error?: string }>;
};


export type CreateLeaveTypePayload = {
  organizationId: number;
  name: string;
  description?: string;
  unit: LeaveUnitValue;
  requiresAttachment: boolean;
  isActive: boolean;
};

export type UpdateLeaveTypePayload = {
  name: string;
  description?: string;
  unit: LeaveUnitValue;
  requiresAttachment: boolean;
  isActive: boolean;
};

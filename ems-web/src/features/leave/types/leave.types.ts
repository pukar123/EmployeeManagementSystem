export type LeaveUnitValue = 1 | 2;

export type LeaveRequestStatusValue = 1 | 2 | 3 | 4 | 5;

export const LeaveRequestStatus = {
  Pending: 1,
  Approved: 2,
  Rejected: 3,
  Cancelled: 4,
  ModifiedPending: 5,
} as const;

const leaveRequestStatusLabels: Record<LeaveRequestStatusValue, string> = {
  1: "Pending",
  2: "Approved",
  3: "Rejected",
  4: "Cancelled",
  5: "Modified (pending)",
};

const leaveRequestStatusByName: Record<string, LeaveRequestStatusValue> = {
  Pending: 1,
  Approved: 2,
  Rejected: 3,
  Cancelled: 4,
  ModifiedPending: 5,
};

/** Normalises a status that may arrive as the numeric enum value or its name. */
export function toLeaveRequestStatusValue(status: number | string): LeaveRequestStatusValue | null {
  if (typeof status === "number") {
    return status >= 1 && status <= 5 ? (status as LeaveRequestStatusValue) : null;
  }
  return leaveRequestStatusByName[status] ?? null;
}

export function leaveRequestStatusLabel(status: number | string): string {
  const value = toLeaveRequestStatusValue(status);
  return value == null ? String(status) : leaveRequestStatusLabels[value];
}

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
  /** API sends the numeric enum value (see LeaveRequestStatus); older code compared names. */
  status: LeaveRequestStatusValue | string;
  submittedAtUtc: string;
  reviewedAtUtc?: string | null;
  reviewedByEmployeeId?: number | null;
  /** Reviewer comment: the rejection reason, or an optional approval comment. */
  decisionComment?: string | null;
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

export type ApproveLeaveRequestPayload = {
  comment?: string;
};

export type RejectLeaveRequestPayload = {
  reason: string;
};

/** A leave request attachment. Download it through `downloadUrl` with `downloadApiFile`; storage paths are never sent. */
export type LeaveAttachment = {
  id: number;
  leaveRequestId: number;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  downloadUrl: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

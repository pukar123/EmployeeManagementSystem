/** Matches `EMS.Domain.Enums.ShiftStatus` JSON (numeric). */
export type ShiftStatus = 0 | 1 | 2 | 3;

export type ShiftItem = {
  id: number;
  organizationId: number;
  employeeId: number;
  siteId: number | null;
  title: string;
  description: string | null;
  startAtUtc: string;
  endAtUtc: string;
  status: ShiftStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateShiftRequest = {
  organizationId: number;
  employeeId: number;
  siteId?: number | null;
  title: string;
  description?: string | null;
  startAtUtc: string;
  endAtUtc: string;
};

export type UpdateShiftRequest = {
  siteId?: number | null;
  title: string;
  description?: string | null;
  startAtUtc: string;
  endAtUtc: string;
  status: ShiftStatus;
};

export type ShiftQueryParams = {
  organizationId?: number | null;
  employeeId?: number | null;
};

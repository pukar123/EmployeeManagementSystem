import type { EmploymentStatusValue } from "./employment-status";

/** Aligns with EMS.Application.DTOs.Employee.EmployeeResponseModel (camelCase JSON). */
export type Employee = {
  id: number;
  organizationId: number;
  departmentId: number | null;
  locationId: number | null;
  managerId: number | null;
  jobPositionId: number | null;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  dateOfBirth: string;
  dateJoined: string;
  employmentStatus: EmploymentStatusValue;
  /** Derived from employment lifecycle; true when status is Active. */
  isActive: boolean;
  isArchived: boolean;
  archivedAtUtc: string | null;
  retentionUntilUtc: string | null;
  archiveReason: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateEmployeeRequest = {
  organizationId: number;
  departmentId?: number | null;
  locationId?: number | null;
  managerId?: number | null;
  jobPositionId?: number | null;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  dateOfBirth: string;
  dateJoined: string;
  employmentStatus: EmploymentStatusValue;
};


export type UpdateEmployeeRequest = CreateEmployeeRequest;

export type ProvisionEmployeeUserResponse = {
  employeeName: string;
  employeeNumber: string;
  email: string;
  temporaryPassword: string | null;
  isNewUser: boolean;
  assignedRoleIds: number[];
};

export type AssignEmployeeUserRolesRequest = {
  roleIds: number[];
};

export type EmployeeHistoryResponse = {
  employeeId: number;
  positionHistory: PositionHistoryItem[];
  departmentHistory: DepartmentHistoryItem[];
  managerHistory: ManagerHistoryItem[];
};

export type PositionHistoryItem = {
  id: number;
  previousJobPositionId: number | null;
  newJobPositionId: number | null;
  effectiveFromUtc: string;
  effectiveToUtc: string | null;
  reason: string | null;
  changedByUserId: number | null;
  changedByUserName: string | null;
  changedByEmail: string | null;
  createdAtUtc: string;
};

export type DepartmentHistoryItem = {
  id: number;
  previousDepartmentId: number | null;
  newDepartmentId: number | null;
  effectiveFromUtc: string;
  effectiveToUtc: string | null;
  reason: string | null;
  changedByUserId: number | null;
  changedByUserName: string | null;
  changedByEmail: string | null;
  createdAtUtc: string;
};

export type ManagerHistoryItem = {
  id: number;
  previousManagerId: number | null;
  newManagerId: number | null;
  effectiveFromUtc: string;
  effectiveToUtc: string | null;
  reason: string | null;
  changedByUserId: number | null;
  changedByUserName: string | null;
  changedByEmail: string | null;
  createdAtUtc: string;
};

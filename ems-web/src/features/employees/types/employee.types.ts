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

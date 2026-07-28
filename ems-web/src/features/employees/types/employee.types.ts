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
  onboardingTemplateId?: number | null;
  generateOnboardingTasks?: boolean;
  positionEffectiveFromUtc?: string | null;
  positionChangeReason?: string | null;
  departmentEffectiveFromUtc?: string | null;
  departmentChangeReason?: string | null;
  managerEffectiveFromUtc?: string | null;
  managerChangeReason?: string | null;
};


export type UpdateEmployeeRequest = CreateEmployeeRequest;

export type ProvisionEmployeeUserResponse = {
  employeeName: string;
  employeeNumber: string;
  email: string;
  isNewUser: boolean;
  assignedRoleKeys: string[];
};

export type AssignEmployeeUserRolesRequest = {
  roleKeys: string[];
};

export type EmployeeHistoryResponse = {
  employeeId: number;
  positionHistory: PositionHistoryItem[];
  departmentHistory: DepartmentHistoryItem[];
  managerHistory: ManagerHistoryItem[];
  employmentStatusHistory: EmploymentStatusHistoryItem[];
};

export type PositionHistoryItem = {
  id: number;
  previousJobPositionId: number | null;
  previousJobPositionTitle: string | null;
  newJobPositionId: number | null;
  newJobPositionTitle: string | null;
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
  previousDepartmentName: string | null;
  newDepartmentId: number | null;
  newDepartmentName: string | null;
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
  previousManagerName: string | null;
  previousManagerEmployeeNumber: string | null;
  newManagerId: number | null;
  newManagerName: string | null;
  newManagerEmployeeNumber: string | null;
  effectiveFromUtc: string;
  effectiveToUtc: string | null;
  reason: string | null;
  changedByUserId: number | null;
  changedByUserName: string | null;
  changedByEmail: string | null;
  createdAtUtc: string;
};

export type EmploymentStatusHistoryItem = {
  id: number;
  previousStatus: EmploymentStatusValue | null;
  newStatus: EmploymentStatusValue;
  effectiveDateUtc: string;
  reason: string | null;
  changedByUserId: number | null;
  changedByUserName: string | null;
  changedByEmail: string | null;
  createdAtUtc: string;
};

export type EmployeeEffectiveRole = {
  roleKey: string;
  roleName: string;
  roleNormalizedName: string;
  source: "position_inherited" | "direct_override";
  jobPositionId: number | null;
  jobPositionTitle: string | null;
  isSystem: boolean;
};

export type SetEmployeeDirectRolesRequest = {
  roleKeys: string[];
};

export type LinkEmployeeUserRequest = {
  userId: number;
};

export type TransferEmployeeDepartmentRequest = {
  newDepartmentId?: number | null;
  effectiveFromUtc: string;
  reason?: string | null;
};

export type TransferEmployeePositionRequest = {
  newJobPositionId?: number | null;
  effectiveFromUtc: string;
  reason?: string | null;
};

export type TransferEmployeeManagerRequest = {
  newManagerId?: number | null;
  effectiveFromUtc: string;
  reason?: string | null;
};

export type EmployeeDirectoryQuery = {
  organizationId: number;
  page?: number;
  pageSize?: number;
  search?: string;
  employmentStatus?: EmploymentStatusValue | null;
  departmentId?: number | null;
  jobPositionId?: number | null;
  managerId?: number | null;
  siteId?: number | null;
  loginLinkStatus?: "linked" | "not_linked" | "disabled" | null;
  isArchived?: boolean;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
};

export type EmployeeDirectoryItem = {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  employmentStatus: EmploymentStatusValue;
  isActive: boolean;
  isArchived: boolean;
  departmentId: number | null;
  departmentName: string | null;
  jobPositionId: number | null;
  jobPositionTitle: string | null;
  managerId: number | null;
  managerName: string | null;
  managerEmployeeNumber: string | null;
  primarySiteName: string | null;
  hasLinkedLogin: boolean;
  linkedLoginIsActive: boolean | null;
};

export type PagedEmployeeDirectory = {
  items: EmployeeDirectoryItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export type EmployeeProfile = {
  id: number;
  organizationId: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  dateOfBirth: string;
  dateJoined: string;
  employmentStatus: EmploymentStatusValue;
  isActive: boolean;
  isArchived: boolean;
  archivedAtUtc: string | null;
  retentionUntilUtc: string | null;
  archiveReason: string | null;
  departmentId: number | null;
  departmentName: string | null;
  jobPositionId: number | null;
  jobPositionTitle: string | null;
  jobPositionCode: string | null;
  managerId: number | null;
  managerName: string | null;
  managerEmployeeNumber: string | null;
  locationId: number | null;
  locationLabel: string | null;
  primarySiteName: string | null;
  siteNames: string[];
  hasLinkedLogin: boolean;
  linkedUserId: number | null;
  linkedUserEmail: string | null;
  linkedLoginIsActive: boolean | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type EmployeeScheduledChange = {
  id: number;
  employeeId: number;
  changeType: number;
  targetReferenceId: number | null;
  targetStatus: number | null;
  effectiveAtUtc: string;
  reason: string | null;
  status: number;
  createdAtUtc: string;
  processedAtUtc: string | null;
  failureReason: string | null;
};

export type CreateEmployeeScheduledChangeRequest = {
  changeType: number;
  effectiveAtUtc: string;
  reason?: string | null;
  targetReferenceId?: number | null;
  targetStatus?: number | null;
};

export type EmployeeInvitation = {
  id: number;
  employeeId: number;
  expiresAtUtc: string;
  usedAtUtc: string | null;
  revokedAtUtc: string | null;
  lastSentAtUtc: string | null;
  deliveryStatus: number;
  deliveryFailureReason: string | null;
  canResend: boolean;
  resendCooldownSecondsRemaining: number;
};

export type PossibleDuplicateEmployee = {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  dateOfBirth: string;
  matchReason: string;
};

export type TerminateEmployeeRequest = {
  effectiveDateUtc: string;
  reason: string;
};

export type ArchiveEmployeeRequest = {
  reason: string;
};

export type ChangeEmploymentStatusRequest = {
  newStatus: EmploymentStatusValue;
  effectiveDateUtc: string;
  reason?: string | null;
};

/** Aligns with EMS.Application.DTOs.JobPosition (camelCase JSON). */
export type JobPosition = {
  id: number;
  organizationId: number;
  title: string;
  description: string | null;
  code: string | null;
  isActive: boolean;
};

export type CreateJobPositionRequest = {
  organizationId: number;
  title: string;
  description?: string | null;
  code?: string | null;
  isActive: boolean;
};

export type PositionRole = {
  roleId: number;
  roleName: string;
  roleNormalizedName: string;
  isSystem: boolean;
};

export type SetPositionRolesRequest = {
  roleIds: number[];
};

export type UpdateJobPositionRequest = {
  title: string;
  description?: string | null;
  code?: string | null;
  isActive: boolean;
};

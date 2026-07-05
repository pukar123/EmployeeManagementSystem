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
  roleKey: string;
  roleName: string;
  roleNormalizedName: string;
  isSystem: boolean;
};

export type SetPositionRolesRequest = {
  roleKeys: string[];
};

export type UpdateJobPositionRequest = {
  title: string;
  description?: string | null;
  code?: string | null;
  isActive: boolean;
};

import { emsHttpClient } from "@/shared/api/http-client";
import type {
  CreateJobPositionRequest,
  JobPosition,
  PositionRole,
  SetPositionRolesRequest,
  UpdateJobPositionRequest,
} from "../types/job-position.types";

const PATH = "/api/JobPositions";

export const jobPositionService = {
  getByOrganization: async (organizationId: number): Promise<JobPosition[]> => {
    const { data } = await emsHttpClient.get<JobPosition[]>(PATH, {
      params: { organizationId },
    });
    return data;
  },

  createJobPosition: async (body: CreateJobPositionRequest): Promise<JobPosition> => {
    const { data } = await emsHttpClient.post<JobPosition>(PATH, body);
    return data;
  },

  updateJobPosition: async (id: number, body: UpdateJobPositionRequest): Promise<JobPosition> => {
    const { data } = await emsHttpClient.put<JobPosition>(`${PATH}/${id}`, body);
    return data;
  },

  deleteJobPosition: async (id: number): Promise<void> => {
    await emsHttpClient.delete(`${PATH}/${id}`);
  },

  getPositionRoles: async (id: number): Promise<PositionRole[]> => {
    const { data } = await emsHttpClient.get<PositionRole[]>(`${PATH}/${id}/roles`);
    return data;
  },

  setPositionRoles: async (id: number, body: SetPositionRolesRequest): Promise<void> => {
    await emsHttpClient.put(`${PATH}/${id}/roles`, body);
  },
};

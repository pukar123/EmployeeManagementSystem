import { emsHttpClient, postFormData } from "@/shared/api/http-client";
import type {
  CreateOrganizationRequest,
  Organization,
  UpdateOrganizationRequest,
} from "../types/organization.types";

const PATH = "/api/Organizations";

export const organizationService = {
  getOrganizations: async (): Promise<Organization[]> => {
    const { data } = await emsHttpClient.get<Organization[]>(PATH);
    return data;
  },

  createOrganization: async (body: CreateOrganizationRequest): Promise<Organization> => {
    const { data } = await emsHttpClient.post<Organization>(PATH, body);
    return data;
  },

  updateOrganization: async (id: number, body: UpdateOrganizationRequest): Promise<Organization> => {
    const { data } = await emsHttpClient.put<Organization>(`${PATH}/${id}`, body);
    return data;
  },

  /** Multipart field name must be `file` (matches API). Logo bytes live on disk; DB stores `logoRelativePath` only. */
  uploadOrganizationLogo: async (id: number, file: File): Promise<Organization> => {
    const formData = new FormData();
    formData.append("file", file);
    return postFormData<Organization>(`${PATH}/${id}/logo`, formData);
  },
};

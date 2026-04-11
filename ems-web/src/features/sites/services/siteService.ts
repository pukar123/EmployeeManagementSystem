import { httpClient } from "@/shared/api/http-client";
import type { CreateSiteRequest, Site, UpdateSiteRequest } from "../types/site.types";

const PATH = "/api/Sites";

export const siteService = {
  getSites: async (): Promise<Site[]> => {
    const { data } = await httpClient.get<Site[]>(PATH);
    return data;
  },

  createSite: async (body: CreateSiteRequest): Promise<Site> => {
    const { data } = await httpClient.post<Site>(PATH, body);
    return data;
  },

  updateSite: async (id: number, body: UpdateSiteRequest): Promise<Site> => {
    const { data } = await httpClient.put<Site>(`${PATH}/${id}`, body);
    return data;
  },

  deleteSite: async (id: number): Promise<void> => {
    await httpClient.delete(`${PATH}/${id}`);
  },
};

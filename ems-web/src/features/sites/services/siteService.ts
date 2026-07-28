import { emsHttpClient } from "@/shared/api/http-client";
import type { CreateSiteRequest, Site, UpdateSiteRequest } from "../types/site.types";

const PATH = "/api/Sites";

export const siteService = {
  getSites: async (): Promise<Site[]> => {
    const { data } = await emsHttpClient.get<Site[]>(PATH);
    return data;
  },

  createSite: async (body: CreateSiteRequest): Promise<Site> => {
    const { data } = await emsHttpClient.post<Site>(PATH, body);
    return data;
  },

  updateSite: async (id: number, body: UpdateSiteRequest): Promise<Site> => {
    const { data } = await emsHttpClient.put<Site>(`${PATH}/${id}`, body);
    return data;
  },

  deleteSite: async (id: number): Promise<void> => {
    await emsHttpClient.delete(`${PATH}/${id}`);
  },
};

import { emsHttpClient } from "@/shared/api/http-client";
import type { Location } from "../types/location.types";

const PATH = "/api/Locations";

export const locationService = {
  getLocations: async (): Promise<Location[]> => {
    const { data } = await emsHttpClient.get<Location[]>(PATH);
    return data;
  },
};

import { httpClient } from "@/shared/api/http-client";
import type { EmployeePortalSummary, PortalShift } from "../types/employee-portal.types";

const PATH = "/api/EmployeePortal";

export const employeePortalService = {
  getPortal: async (): Promise<EmployeePortalSummary> => {
    const { data } = await httpClient.get<EmployeePortalSummary>(PATH);
    return data;
  },

  startShift: async (shiftId: number): Promise<PortalShift> => {
    const { data } = await httpClient.post<PortalShift>(`${PATH}/shifts/${shiftId}/start`);
    return data;
  },
};

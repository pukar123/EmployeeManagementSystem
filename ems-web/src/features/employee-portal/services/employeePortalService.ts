import { httpClient } from "@/shared/api/http-client";
import type { EmployeePortalEligibility, EmployeePortalSummary, PortalShift } from "../types/employee-portal.types";

const PATH = "/api/EmployeePortal";

export const employeePortalService = {
  getEligibility: async (): Promise<EmployeePortalEligibility> => {
    const { data } = await httpClient.get<EmployeePortalEligibility>(`${PATH}/eligibility`);
    return data;
  },

  getPortal: async (): Promise<EmployeePortalSummary> => {
    const { data } = await httpClient.get<EmployeePortalSummary>(PATH);
    return data;
  },

  startShift: async (shiftId: number): Promise<PortalShift> => {
    const { data } = await httpClient.post<PortalShift>(`${PATH}/shifts/${shiftId}/start`);
    return data;
  },
};

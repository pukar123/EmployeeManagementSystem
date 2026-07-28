import { emsHttpClient } from "@/shared/api/http-client";
import type { EmployeeOnboardingProgress } from "../types/onboarding.types";

export const employeeOnboardingService = {
  getProgress: async (employeeId: number): Promise<EmployeeOnboardingProgress> => {
    const { data } = await emsHttpClient.get<EmployeeOnboardingProgress>(`/api/employees/${employeeId}/onboarding`);
    return data;
  },
};

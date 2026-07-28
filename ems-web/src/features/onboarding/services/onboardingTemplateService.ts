import { emsHttpClient } from "@/shared/api/http-client";
import type {
  CreateOnboardingChecklistTemplatePayload,
  OnboardingChecklistTemplate,
  UpdateOnboardingChecklistTemplatePayload,
} from "../types/onboarding.types";

const PATH = "/api/onboarding/templates";

export const onboardingTemplateService = {
  getByOrganization: async (organizationId: number): Promise<OnboardingChecklistTemplate[]> => {
    const { data } = await emsHttpClient.get<OnboardingChecklistTemplate[]>(PATH, {
      params: { organizationId },
    });
    return data;
  },

  getById: async (id: number): Promise<OnboardingChecklistTemplate> => {
    const { data } = await emsHttpClient.get<OnboardingChecklistTemplate>(`${PATH}/${id}`);
    return data;
  },

  create: async (payload: CreateOnboardingChecklistTemplatePayload): Promise<OnboardingChecklistTemplate> => {
    const { data } = await emsHttpClient.post<OnboardingChecklistTemplate>(PATH, payload);
    return data;
  },

  update: async (
    id: number,
    payload: UpdateOnboardingChecklistTemplatePayload,
  ): Promise<OnboardingChecklistTemplate> => {
    const { data } = await emsHttpClient.put<OnboardingChecklistTemplate>(`${PATH}/${id}`, payload);
    return data;
  },

  delete: async (id: number): Promise<void> => {
    await emsHttpClient.delete(`${PATH}/${id}`);
  },
};

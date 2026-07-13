import { useMutation, useQueryClient } from "@tanstack/react-query";
import { onboardingTemplateService } from "../services/onboardingTemplateService";
import { onboardingKeys } from "../services/query-keys";
import type {
  CreateOnboardingChecklistTemplatePayload,
  UpdateOnboardingChecklistTemplatePayload,
} from "../types/onboarding.types";

export function useOnboardingTemplateMutations(organizationId: number | null) {
  const queryClient = useQueryClient();

  const refresh = () => {
    if (organizationId == null) return;
    void queryClient.invalidateQueries({ queryKey: onboardingKeys.templates(organizationId) });
  };

  const createTemplate = useMutation({
    mutationFn: (payload: CreateOnboardingChecklistTemplatePayload) => onboardingTemplateService.create(payload),
    onSuccess: refresh,
  });

  const updateTemplate = useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateOnboardingChecklistTemplatePayload }) =>
      onboardingTemplateService.update(id, payload),
    onSuccess: refresh,
  });

  const deleteTemplate = useMutation({
    mutationFn: (id: number) => onboardingTemplateService.delete(id),
    onSuccess: refresh,
  });

  return { createTemplate, updateTemplate, deleteTemplate };
}

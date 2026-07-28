import { useQuery } from "@tanstack/react-query";
import { onboardingTemplateService } from "../services/onboardingTemplateService";
import { onboardingKeys } from "../services/query-keys";

export function useOnboardingTemplates(organizationId: number | null) {
  return useQuery({
    queryKey: onboardingKeys.templates(organizationId ?? 0),
    queryFn: () => onboardingTemplateService.getByOrganization(organizationId ?? 0),
    enabled: organizationId != null,
  });
}

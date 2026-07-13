import { useQuery } from "@tanstack/react-query";
import { employeeOnboardingService } from "../services/employeeOnboardingService";
import { onboardingKeys } from "../services/query-keys";

export function useEmployeeOnboardingProgress(employeeId: number | null, enabled = true) {
  return useQuery({
    queryKey: onboardingKeys.employeeProgress(employeeId ?? 0),
    queryFn: () => employeeOnboardingService.getProgress(employeeId ?? 0),
    enabled: employeeId != null && enabled,
  });
}

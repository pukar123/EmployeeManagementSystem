import { useQuery } from "@tanstack/react-query";
import { employeePortalKeys } from "../services/query-keys";
import { employeePortalService } from "../services/employeePortalService";

export function useEmployeePortalEligibility() {
  return useQuery({
    queryKey: employeePortalKeys.eligibility(),
    queryFn: () => employeePortalService.getEligibility(),
    staleTime: 0,
    refetchOnMount: "always",
  });
}

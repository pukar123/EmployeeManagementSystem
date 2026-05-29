import { useQuery } from "@tanstack/react-query";
import { employeePortalKeys } from "../services/query-keys";
import { employeePortalService } from "../services/employeePortalService";

export function useEmployeePortal() {
  return useQuery({
    queryKey: employeePortalKeys.summary(),
    queryFn: () => employeePortalService.getPortal(),
  });
}

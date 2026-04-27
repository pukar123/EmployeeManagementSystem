import { useQuery } from "@tanstack/react-query";
import { leaveService } from "../services/leaveService";
import { leaveKeys } from "../services/query-keys";

export function useLeaveTypes(organizationId: number | null) {
  return useQuery({
    queryKey: leaveKeys.types(organizationId ?? 0),
    queryFn: () => leaveService.getLeaveTypes(organizationId ?? 0),
    enabled: organizationId != null,
  });
}

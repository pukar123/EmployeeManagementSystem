import { useQuery } from "@tanstack/react-query";
import { leaveService } from "../services/leaveService";
import { leaveKeys } from "../services/query-keys";

export function useLeaveRequests(employeeId: number | null) {
  return useQuery({
    queryKey: leaveKeys.requests(employeeId ?? 0),
    queryFn: () => leaveService.getLeaveRequests(employeeId ?? 0),
    enabled: employeeId != null,
  });
}

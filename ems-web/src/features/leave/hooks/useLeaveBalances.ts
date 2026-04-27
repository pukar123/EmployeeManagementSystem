import { useQuery } from "@tanstack/react-query";
import { leaveService } from "../services/leaveService";
import { leaveKeys } from "../services/query-keys";

export function useLeaveBalances(employeeId: number | null) {
  return useQuery({
    queryKey: leaveKeys.balances(employeeId ?? 0),
    queryFn: () => leaveService.getLeaveBalances(employeeId ?? 0),
    enabled: employeeId != null,
  });
}

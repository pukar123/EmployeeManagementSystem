import { useQuery } from "@tanstack/react-query";
import { leaveService } from "../services/leaveService";
import { leaveKeys } from "../services/query-keys";

function todayIsoDateOnly(): string {
  return new Date().toISOString().slice(0, 10);
}

export function useLeaveAdminSummary(organizationId: number | null) {
  const asOfDateUtc = todayIsoDateOnly();
  return useQuery({
    queryKey: leaveKeys.adminSummary(organizationId ?? 0, asOfDateUtc),
    queryFn: () => leaveService.getAdminSummary(organizationId ?? 0, asOfDateUtc),
    enabled: organizationId != null,
  });
}

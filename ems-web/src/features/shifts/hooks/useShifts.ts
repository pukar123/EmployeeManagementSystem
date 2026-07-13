import { useQuery } from "@tanstack/react-query";
import { shiftKeys } from "../services/query-keys";
import { shiftService } from "../services/shiftService";
import type { ShiftQueryParams } from "../types/shift.types";

export function useShifts(query: ShiftQueryParams = {}) {
  return useQuery({
    queryKey: shiftKeys.list(query),
    queryFn: () => shiftService.getAll(query),
    enabled: query.organizationId != null,
  });
}

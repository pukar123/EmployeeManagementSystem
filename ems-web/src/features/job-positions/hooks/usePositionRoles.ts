import { useQuery } from "@tanstack/react-query";
import { jobPositionService } from "../services/jobPositionService";
import { jobPositionKeys } from "../services/query-keys";

export function usePositionRoles(jobPositionId: number | null) {
  return useQuery({
    queryKey: jobPositionId == null ? jobPositionKeys.all : jobPositionKeys.roles(jobPositionId),
    queryFn: () => jobPositionService.getPositionRoles(jobPositionId!),
    enabled: jobPositionId != null,
  });
}

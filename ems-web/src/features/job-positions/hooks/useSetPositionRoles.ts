import { useMutation, useQueryClient } from "@tanstack/react-query";
import { jobPositionService } from "../services/jobPositionService";
import { jobPositionKeys } from "../services/query-keys";

export function useSetPositionRoles(organizationId: number | null) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, roleKeys }: { id: number; roleKeys: string[] }) =>
      jobPositionService.setPositionRoles(id, { roleKeys }),
    onSuccess: (_, variables) => {
      if (organizationId != null) {
        void queryClient.invalidateQueries({ queryKey: jobPositionKeys.list(organizationId) });
      }
      void queryClient.invalidateQueries({ queryKey: jobPositionKeys.roles(variables.id) });
    },
  });
}

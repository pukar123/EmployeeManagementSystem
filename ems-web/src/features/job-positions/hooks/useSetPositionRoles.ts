import { useMutation, useQueryClient } from "@tanstack/react-query";
import { jobPositionService } from "../services/jobPositionService";
import { jobPositionKeys } from "../services/query-keys";

export function useSetPositionRoles(organizationId: number | null) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, roleIds }: { id: number; roleIds: number[] }) =>
      jobPositionService.setPositionRoles(id, { roleIds }),
    onSuccess: (_, variables) => {
      if (organizationId != null) {
        void queryClient.invalidateQueries({ queryKey: jobPositionKeys.list(organizationId) });
      }
      void queryClient.invalidateQueries({ queryKey: jobPositionKeys.roles(variables.id) });
    },
  });
}

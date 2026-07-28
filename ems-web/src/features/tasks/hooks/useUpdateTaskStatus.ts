import { useMutation, useQueryClient } from "@tanstack/react-query";
import { taskKeys } from "../services/query-keys";
import { onboardingKeys } from "@/features/onboarding/services/query-keys";
import { taskService } from "../services/taskService";

export function useUpdateTaskStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: number }) =>
      taskService.updateTaskStatus(id, { status: status as 1 | 2 | 3 | 4 }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: taskKeys.all });
      await queryClient.invalidateQueries({ queryKey: onboardingKeys.all });
    },
  });
}

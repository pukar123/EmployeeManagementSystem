import { useMutation, useQueryClient } from "@tanstack/react-query";
import { taskKeys } from "../services/query-keys";
import { taskService } from "../services/taskService";

export function useCreateTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: taskService.createTask,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}

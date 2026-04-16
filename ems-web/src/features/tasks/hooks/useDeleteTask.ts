import { useMutation, useQueryClient } from "@tanstack/react-query";
import { taskKeys } from "../services/query-keys";
import { taskService } from "../services/taskService";

export function useDeleteTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: taskService.deleteTask,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}

import { useQuery } from "@tanstack/react-query";
import { taskKeys } from "../services/query-keys";
import { taskService } from "../services/taskService";
import type { TaskQueryParams } from "../types/task.types";

export function useTasks(query: TaskQueryParams = {}) {
  return useQuery({
    queryKey: taskKeys.list(query),
    queryFn: () => taskService.getAll(query),
  });
}

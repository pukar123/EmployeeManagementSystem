import { useQuery } from "@tanstack/react-query";
import { taskKeys } from "../services/query-keys";
import { taskService } from "../services/taskService";

export function useTasks(employeeId?: number | null) {
  return useQuery({
    queryKey: taskKeys.list(employeeId),
    queryFn: () => taskService.getAll(employeeId),
  });
}

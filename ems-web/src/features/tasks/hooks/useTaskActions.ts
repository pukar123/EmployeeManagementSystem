import { useCallback } from "react";
import { toast } from "sonner";
import { getErrorMessage } from "@/shared/api/http-client";
import { useCreateTask } from "./useCreateTask";
import { useDeleteTask } from "./useDeleteTask";
import { useUpdateTaskStatus } from "./useUpdateTaskStatus";
import type { CreateTaskRequest, TaskItem, TaskWorkflowStatus } from "../types/task.types";

type UseTaskActionsOptions = {
  onCreateSuccess?: () => void;
};

export function useTaskActions({ onCreateSuccess }: UseTaskActionsOptions = {}) {
  const createMut = useCreateTask();
  const statusMut = useUpdateTaskStatus();
  const deleteMut = useDeleteTask();

  const createTask = useCallback(
    async (payload: CreateTaskRequest) => {
      try {
        await createMut.mutateAsync(payload);
        toast.success("Task assigned.");
        onCreateSuccess?.();
      } catch (err) {
        toast.error(getErrorMessage(err));
      }
    },
    [createMut, onCreateSuccess],
  );

  const updateTaskStatus = useCallback(async (task: TaskItem, nextStatus: TaskWorkflowStatus) => {
    try {
      await statusMut.mutateAsync({ id: task.id, status: nextStatus });
      toast.success("Task status updated.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }, [statusMut]);

  const deleteTask = useCallback(async (task: TaskItem) => {
    if (!window.confirm(`Delete task "${task.title}"?`)) {
      return;
    }

    try {
      await deleteMut.mutateAsync(task.id);
      toast.success("Task deleted.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }, [deleteMut]);

  return {
    createTask,
    updateTaskStatus,
    deleteTask,
    isCreating: createMut.isPending,
  };
}

import { httpClient } from "@/shared/api/http-client";
import type { CreateTaskRequest, TaskItem, UpdateTaskRequest, UpdateTaskStatusRequest } from "../types/task.types";

const PATH = "/api/Tasks";

export const taskService = {
  getAll: async (employeeId?: number | null): Promise<TaskItem[]> => {
    const { data } = await httpClient.get<TaskItem[]>(PATH, {
      params: employeeId ? { employeeId } : undefined,
    });
    return data;
  },

  createTask: async (body: CreateTaskRequest): Promise<TaskItem> => {
    const { data } = await httpClient.post<TaskItem>(PATH, body);
    return data;
  },

  updateTask: async (id: number, body: UpdateTaskRequest): Promise<TaskItem> => {
    const { data } = await httpClient.put<TaskItem>(`${PATH}/${id}`, body);
    return data;
  },

  updateTaskStatus: async (id: number, body: UpdateTaskStatusRequest): Promise<TaskItem> => {
    const { data } = await httpClient.patch<TaskItem>(`${PATH}/${id}/status`, body);
    return data;
  },

  deleteTask: async (id: number): Promise<void> => {
    await httpClient.delete(`${PATH}/${id}`);
  },
};

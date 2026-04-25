import { httpClient } from "@/shared/api/http-client";
import type { CreateTaskRequest, TaskItem, TaskQueryParams, UpdateTaskRequest, UpdateTaskStatusRequest } from "../types/task.types";

const PATH = "/api/Tasks";

export const taskService = {
  getAll: async (query: TaskQueryParams = {}): Promise<TaskItem[]> => {
    const params = {
      ...(query.employeeId ? { employeeId: query.employeeId } : {}),
      ...(query.assignedByUserId ? { assignedByUserId: query.assignedByUserId } : {}),
      ...(query.rangeStartUtc ? { rangeStartUtc: query.rangeStartUtc } : {}),
      ...(query.rangeEndUtc ? { rangeEndUtc: query.rangeEndUtc } : {}),
    };

    const { data } = await httpClient.get<TaskItem[]>(PATH, {
      params: Object.keys(params).length > 0 ? params : undefined,
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

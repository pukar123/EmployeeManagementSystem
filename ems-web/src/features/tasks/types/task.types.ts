export type TaskWorkflowStatus = 1 | 2 | 3 | 4;
export type TaskPriority = 1 | 2 | 3;

export type TaskItem = {
  id: number;
  employeeId: number;
  organizationId: number | null;
  assignedByUserId: number | null;
  title: string;
  description: string | null;
  status: TaskWorkflowStatus;
  priority: TaskPriority | null;
  assignedAtUtc: string;
  dueAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateTaskRequest = {
  employeeId: number;
  organizationId?: number | null;
  title: string;
  description?: string | null;
  dueAtUtc?: string | null;
  priority?: TaskPriority | null;
};

export type UpdateTaskRequest = {
  title: string;
  description?: string | null;
  dueAtUtc?: string | null;
  priority?: TaskPriority | null;
};

export type UpdateTaskStatusRequest = {
  status: TaskWorkflowStatus;
};

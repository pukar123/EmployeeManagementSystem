import type { Employee } from "@/features/employees/types/employee.types";
import type { TaskItem, TaskPriority, TaskWorkflowStatus } from "../types/task.types";

export const taskStatusLabels: Record<TaskWorkflowStatus, string> = {
  1: "Assigned",
  2: "In Progress",
  3: "Blocked",
  4: "Completed",
};

export const taskPriorityLabels: Record<TaskPriority, string> = {
  1: "Low",
  2: "Medium",
  3: "High",
};

export function getEmployeeDisplayLabel(employee: Pick<Employee, "employeeNumber" | "firstName" | "lastName">): string {
  const name = `${employee.firstName} ${employee.lastName}`.trim();
  const employeeNumber = employee.employeeNumber?.trim();
  return employeeNumber ? `${employeeNumber} - ${name}` : name;
}

export function buildEmployeeLabelMap(employees: Employee[]): Map<number, string> {
  const map = new Map<number, string>();
  for (const employee of employees) {
    map.set(employee.id, getEmployeeDisplayLabel(employee));
  }

  return map;
}

export function formatTaskDateTime(value: string | null): string {
  return value ? new Date(value).toLocaleString() : "—";
}

export function matchesTaskSearch(task: TaskItem, searchText: string, employeeLabel: string): boolean {
  if (!searchText) {
    return true;
  }

  const normalized = searchText.trim().toLowerCase();
  if (!normalized) {
    return true;
  }

  const searchableText = `${task.title} ${task.description ?? ""} ${employeeLabel} ${taskStatusLabels[task.status]}`.toLowerCase();
  return searchableText.includes(normalized);
}

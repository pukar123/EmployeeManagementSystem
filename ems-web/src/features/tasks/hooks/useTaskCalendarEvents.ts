import { useMemo } from "react";
import type { Event } from "react-big-calendar";
import type { TaskItem } from "../types/task.types";

export type TaskCalendarEvent = Event & {
  resource: TaskItem;
};

type UseTaskCalendarEventsOptions = {
  tasks: TaskItem[];
  employeeLabelById: Map<number, string>;
};

export function useTaskCalendarEvents({ tasks, employeeLabelById }: UseTaskCalendarEventsOptions) {
  return useMemo<TaskCalendarEvent[]>(
    () =>
      tasks.map((task) => {
        const fallbackDate = task.assignedAtUtc;
        const start = new Date(task.startAtUtc ?? task.dueAtUtc ?? fallbackDate);
        const end = new Date(task.dueAtUtc ?? task.startAtUtc ?? fallbackDate);
        const employee = employeeLabelById.get(task.employeeId) ?? "Unknown employee";

        return {
          title: `${task.title} | ${employee}`,
          start,
          end,
          allDay: false,
          resource: task,
        };
      }),
    [employeeLabelById, tasks],
  );
}

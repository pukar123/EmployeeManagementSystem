"use client";

import { Calendar, dateFnsLocalizer, type View } from "react-big-calendar";
import { format, getDay, parse, startOfWeek } from "date-fns";
import { enUS } from "date-fns/locale";
import "react-big-calendar/lib/css/react-big-calendar.css";
import type { TaskCalendarEvent } from "../hooks/useTaskCalendarEvents";
import { taskPriorityLabels, taskStatusLabels } from "../utils/taskDisplay";

const locales = {
  "en-US": enUS,
};

const localizer = dateFnsLocalizer({
  format,
  parse,
  startOfWeek,
  getDay,
  locales,
});

type TaskCalendarViewProps = {
  events: TaskCalendarEvent[];
  employeeLabelById: Map<number, string>;
  defaultView?: View;
  onRangeChange: (range: { start: Date; end: Date }) => void;
};

export function TaskCalendarView({ events, employeeLabelById, onRangeChange, defaultView = "month" }: TaskCalendarViewProps) {
  return (
    <div className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-700 dark:bg-zinc-950">
      <div className="h-[700px]">
        <Calendar
          localizer={localizer}
          events={events}
          startAccessor="start"
          endAccessor="end"
          tooltipAccessor={(event) => {
            const task = (event as TaskCalendarEvent).resource;
            const employee = employeeLabelById.get(task.employeeId) ?? "Unknown employee";
            const status = taskStatusLabels[task.status];
            const priority = task.priority ? taskPriorityLabels[task.priority] : "No priority";
            return `${task.title}\n${employee}\n${status}\n${priority}`;
          }}
          defaultView={defaultView}
          views={["month", "week", "day", "agenda"]}
          onRangeChange={(range) => {
            if (Array.isArray(range) && range.length > 0) {
              onRangeChange({ start: range[0], end: range[range.length - 1] });
              return;
            }

            if (!Array.isArray(range) && "start" in range && "end" in range) {
              onRangeChange({ start: range.start, end: range.end });
            }
          }}
        />
      </div>
    </div>
  );
}

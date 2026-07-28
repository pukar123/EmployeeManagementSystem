"use client";

import { Calendar, dateFnsLocalizer, type View } from "react-big-calendar";
import { format, getDay, parse, startOfWeek } from "date-fns";
import { enUS } from "date-fns/locale";
import "react-big-calendar/lib/css/react-big-calendar.css";
import { shiftCalendarTooltip, type ShiftCalendarEvent } from "../hooks/useShiftCalendarEvents";
import type { ShiftStatus } from "../types/shift.types";
import { shiftStatusLabels } from "../utils/shiftDisplay";

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

type ShiftCalendarViewProps = {
  events: ShiftCalendarEvent[];
  employeeLabelById: Map<number, string>;
  defaultView?: View;
  onRangeChange: (range: { start: Date; end: Date }) => void;
};

export function ShiftCalendarView({ events, employeeLabelById, onRangeChange, defaultView = "month" }: ShiftCalendarViewProps) {
  return (
    <div className="rounded-xl border border-border bg-card p-3 dark:bg-card">
      <div className="h-[700px]">
        <Calendar
          localizer={localizer}
          events={events}
          startAccessor="start"
          endAccessor="end"
          tooltipAccessor={(event) => {
            const shift = (event as ShiftCalendarEvent).resource;
            const employee = employeeLabelById.get(shift.employeeId) ?? "Unknown employee";
            return shiftCalendarTooltip(shift, employee);
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
          eventPropGetter={(event) => {
            const status = (event as ShiftCalendarEvent).resource.status as ShiftStatus;
            const palette: Record<ShiftStatus, { backgroundColor: string; borderColor: string }> = {
              0: { backgroundColor: "#64748b", borderColor: "#475569" },
              1: { backgroundColor: "#2563eb", borderColor: "#1d4ed8" },
              2: { backgroundColor: "#059669", borderColor: "#047857" },
              3: { backgroundColor: "#d97706", borderColor: "#b45309" },
            };
            const colors = palette[status];
            return {
              style: {
                backgroundColor: colors.backgroundColor,
                borderColor: colors.borderColor,
                color: "#fff",
              },
            };
          }}
        />
      </div>
      <p className="mt-2 text-xs text-muted-foreground">
        Legend: {Object.values(shiftStatusLabels).join(" · ")}
      </p>
    </div>
  );
}

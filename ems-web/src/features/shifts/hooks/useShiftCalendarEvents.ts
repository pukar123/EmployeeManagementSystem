import { useMemo } from "react";
import type { Event } from "react-big-calendar";
import type { ShiftItem } from "../types/shift.types";
import { shiftStatusLabels } from "../utils/shiftDisplay";

export type ShiftCalendarEvent = Event & {
  resource: ShiftItem;
};

type UseShiftCalendarEventsOptions = {
  shifts: ShiftItem[];
  employeeLabelById: Map<number, string>;
};

export function useShiftCalendarEvents({ shifts, employeeLabelById }: UseShiftCalendarEventsOptions) {
  return useMemo<ShiftCalendarEvent[]>(
    () =>
      shifts.map((shift) => {
        const employee = employeeLabelById.get(shift.employeeId) ?? "Unknown employee";
        return {
          title: `${shift.title} | ${employee}`,
          start: new Date(shift.startAtUtc),
          end: new Date(shift.endAtUtc),
          allDay: false,
          resource: shift,
        };
      }),
    [employeeLabelById, shifts],
  );
}

export function shiftCalendarTooltip(shift: ShiftItem, employeeLabel: string): string {
  return `${shift.title}\n${employeeLabel}\n${shiftStatusLabels[shift.status]}`;
}

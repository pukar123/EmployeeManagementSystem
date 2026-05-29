import type { ShiftStatusValue } from "../types/employee-portal.types";

const labels: Record<ShiftStatusValue, string> = {
  0: "Scheduled",
  1: "Started",
  2: "Completed",
  3: "Cancelled",
};

export function shiftStatusLabel(status: ShiftStatusValue): string {
  return labels[status] ?? String(status);
}

import type { Employee } from "@/features/employees/types/employee.types";
import type { Site } from "@/features/sites/types/site.types";
import type { ShiftItem, ShiftStatus } from "../types/shift.types";

export const shiftStatusLabels: Record<ShiftStatus, string> = {
  0: "Scheduled",
  1: "Started",
  2: "Completed",
  3: "Cancelled",
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

export function buildSiteLabelMap(sites: Site[]): Map<number, string> {
  const map = new Map<number, string>();
  for (const site of sites) {
    map.set(site.siteId, site.siteName);
  }
  return map;
}

export function formatShiftDateTime(value: string | null): string {
  return value ? new Date(value).toLocaleString() : "—";
}

export function toDatetimeLocalValue(isoUtc: string): string {
  const date = new Date(isoUtc);
  const pad = (part: number) => String(part).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function shiftOverlapsRange(shift: ShiftItem, rangeStart: Date | null, rangeEnd: Date | null): boolean {
  if (!rangeStart && !rangeEnd) {
    return true;
  }

  const shiftStart = new Date(shift.startAtUtc);
  const shiftEnd = new Date(shift.endAtUtc);
  const from = rangeStart ?? new Date(-8640000000000000);
  const to = rangeEnd ?? new Date(8640000000000000);
  return shiftStart < to && shiftEnd > from;
}

export function matchesShiftSearch(shift: ShiftItem, searchText: string, employeeLabel: string, siteLabel: string): boolean {
  if (!searchText.trim()) {
    return true;
  }

  const normalized = searchText.trim().toLowerCase();
  const searchableText = `${shift.title} ${shift.description ?? ""} ${employeeLabel} ${siteLabel} ${shiftStatusLabels[shift.status]}`.toLowerCase();
  return searchableText.includes(normalized);
}

export function filterShifts(options: {
  shifts: ShiftItem[];
  search: string;
  status: ShiftStatus | null;
  rangeStart: Date | null;
  rangeEnd: Date | null;
  employeeLabelById: Map<number, string>;
  siteLabelById: Map<number, string>;
}): ShiftItem[] {
  const { shifts, search, status, rangeStart, rangeEnd, employeeLabelById, siteLabelById } = options;

  return shifts.filter((shift) => {
    if (status != null && shift.status !== status) {
      return false;
    }

    if (!shiftOverlapsRange(shift, rangeStart, rangeEnd)) {
      return false;
    }

    const employeeLabel = employeeLabelById.get(shift.employeeId) ?? "";
    const siteLabel = shift.siteId != null ? (siteLabelById.get(shift.siteId) ?? "") : "";
    return matchesShiftSearch(shift, search, employeeLabel, siteLabel);
  });
}

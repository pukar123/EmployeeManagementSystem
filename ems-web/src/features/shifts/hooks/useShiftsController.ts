import { useMemo, useState } from "react";
import { useEmployees } from "@/features/employees/hooks";
import { useSites } from "@/features/sites/hooks";
import type { ShiftStatus } from "../types/shift.types";
import { buildEmployeeLabelMap, buildSiteLabelMap, filterShifts } from "../utils/shiftDisplay";
import { useShifts } from "./useShifts";

type UseShiftsControllerOptions = {
  initialViewMode?: "table" | "calendar";
  organizationId: number | null;
};

export function useShiftsController({ initialViewMode = "table", organizationId }: UseShiftsControllerOptions) {
  const [search, setSearch] = useState("");
  const [viewMode, setViewMode] = useState<"table" | "calendar">(initialViewMode);
  const [filterEmployeeId, setFilterEmployeeId] = useState<number | null>(null);
  const [filterStatus, setFilterStatus] = useState<ShiftStatus | null>(null);
  const [filterStartDate, setFilterStartDate] = useState("");
  const [filterEndDate, setFilterEndDate] = useState("");
  const [calendarRange, setCalendarRange] = useState<{ start: Date; end: Date } | null>(null);

  const employeesQuery = useEmployees();
  const sitesQuery = useSites();
  const employees = useMemo(() => employeesQuery.data ?? [], [employeesQuery.data]);
  const sites = useMemo(() => sitesQuery.data ?? [], [sitesQuery.data]);

  const shiftsQuery = useShifts({
    organizationId,
    employeeId: filterEmployeeId,
  });

  const shifts = useMemo(() => shiftsQuery.data ?? [], [shiftsQuery.data]);
  const employeeLabelById = useMemo(() => buildEmployeeLabelMap(employees), [employees]);
  const siteLabelById = useMemo(() => buildSiteLabelMap(sites), [sites]);

  const filteredShifts = useMemo(() => {
    const tableRangeStart = filterStartDate ? new Date(`${filterStartDate}T00:00:00`) : null;
    const tableRangeEnd = filterEndDate ? new Date(`${filterEndDate}T23:59:59.999`) : null;
    const rangeStart = viewMode === "calendar" ? (calendarRange?.start ?? null) : tableRangeStart;
    const rangeEnd = viewMode === "calendar" ? (calendarRange?.end ?? null) : tableRangeEnd;

    return filterShifts({
      shifts,
      search,
      status: filterStatus,
      rangeStart,
      rangeEnd,
      employeeLabelById,
      siteLabelById,
    });
  }, [
    calendarRange,
    employeeLabelById,
    filterEndDate,
    filterStartDate,
    filterStatus,
    search,
    shifts,
    siteLabelById,
    viewMode,
  ]);

  return {
    search,
    setSearch,
    viewMode,
    setViewMode,
    filterEmployeeId,
    setFilterEmployeeId,
    filterStatus,
    setFilterStatus,
    filterStartDate,
    setFilterStartDate,
    filterEndDate,
    setFilterEndDate,
    calendarRange,
    setCalendarRange,
    employees,
    sites,
    employeeLabelById,
    siteLabelById,
    shifts: filteredShifts,
    shiftsQuery,
    employeesQuery,
    sitesQuery,
  };
}

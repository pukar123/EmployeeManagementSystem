import { useMemo, useState } from "react";
import { useEmployees } from "@/features/employees/hooks";
import type { TaskItem } from "../types/task.types";
import { useTasks } from "./useTasks";
import { buildEmployeeLabelMap, matchesTaskSearch } from "../utils/taskDisplay";

type UseTasksControllerOptions = {
  initialViewMode?: "table" | "calendar";
};

export function useTasksController({ initialViewMode = "table" }: UseTasksControllerOptions = {}) {
  const [search, setSearch] = useState("");
  const [viewMode, setViewMode] = useState<"table" | "calendar">(initialViewMode);
  const [filterEmployeeId, setFilterEmployeeId] = useState<number | null>(null);
  const [calendarRange, setCalendarRange] = useState<{ start: Date; end: Date } | null>(null);

  const employeesQuery = useEmployees();
  const employees = useMemo(() => employeesQuery.data ?? [], [employeesQuery.data]);

  const tasksQuery = useTasks({
    employeeId: filterEmployeeId,
    rangeStartUtc: calendarRange?.start.toISOString() ?? null,
    rangeEndUtc: calendarRange?.end.toISOString() ?? null,
  });

  const tasks = useMemo(() => tasksQuery.data ?? [], [tasksQuery.data]);
  const employeeLabelById = useMemo(() => buildEmployeeLabelMap(employees), [employees]);
  const filteredTasks = useMemo(
    () =>
      tasks.filter((task: TaskItem) =>
        matchesTaskSearch(task, search, employeeLabelById.get(task.employeeId) ?? ""),
      ),
    [employeeLabelById, search, tasks],
  );

  return {
    search,
    setSearch,
    viewMode,
    setViewMode,
    filterEmployeeId,
    setFilterEmployeeId,
    calendarRange,
    setCalendarRange,
    employees,
    employeeLabelById,
    tasks: filteredTasks,
    tasksQuery,
    employeesQuery,
  };
}

import type { Employee } from "@/features/employees/types/employee.types";
import { getEmployeeDisplayLabel } from "../utils/taskDisplay";

type TaskFiltersProps = {
  search: string;
  onSearchChange: (value: string) => void;
  employees: Employee[];
  selectedEmployeeId: number | null;
  onSelectedEmployeeChange: (value: number | null) => void;
  inputClassName: string;
};

export function TaskFilters({
  search,
  onSearchChange,
  employees,
  selectedEmployeeId,
  onSelectedEmployeeChange,
  inputClassName,
}: TaskFiltersProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Search
        </label>
        <input
          type="search"
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Title, employee, status"
          className={inputClassName}
        />
      </div>
      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Employee
        </label>
        <select
          value={selectedEmployeeId ?? ""}
          onChange={(e) => onSelectedEmployeeChange(e.target.value ? Number(e.target.value) : null)}
          className={inputClassName}
        >
          <option value="">All employees</option>
          {employees.map((employee) => (
            <option key={employee.id} value={employee.id}>
              {getEmployeeDisplayLabel(employee)}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}

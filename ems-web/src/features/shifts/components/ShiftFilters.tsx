import type { Employee } from "@/features/employees/types/employee.types";
import type { ShiftStatus } from "../types/shift.types";
import { getEmployeeDisplayLabel, shiftStatusLabels } from "../utils/shiftDisplay";

const shiftStatusOptions: ShiftStatus[] = [0, 1, 2, 3];

type ShiftFiltersProps = {
  search: string;
  onSearchChange: (value: string) => void;
  employees: Employee[];
  selectedEmployeeId: number | null;
  onSelectedEmployeeChange: (value: number | null) => void;
  selectedStatus: ShiftStatus | null;
  onSelectedStatusChange: (value: ShiftStatus | null) => void;
  startDate: string;
  onStartDateChange: (value: string) => void;
  endDate: string;
  onEndDateChange: (value: string) => void;
  showDateFilters: boolean;
  inputClassName: string;
};

export function ShiftFilters({
  search,
  onSearchChange,
  employees,
  selectedEmployeeId,
  onSelectedEmployeeChange,
  selectedStatus,
  onSelectedStatusChange,
  startDate,
  onStartDateChange,
  endDate,
  onEndDateChange,
  showDateFilters,
  inputClassName,
}: ShiftFiltersProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">Search</label>
        <input
          type="search"
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Title, employee, status"
          className={inputClassName}
        />
      </div>
      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">Employee</label>
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
      <div>
        <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">Status</label>
        <select
          value={selectedStatus ?? ""}
          onChange={(e) => onSelectedStatusChange(e.target.value === "" ? null : (Number(e.target.value) as ShiftStatus))}
          className={inputClassName}
        >
          <option value="">All statuses</option>
          {shiftStatusOptions.map((value) => (
            <option key={value} value={value}>
              {shiftStatusLabels[value]}
            </option>
          ))}
        </select>
      </div>
      {showDateFilters ? (
        <>
          <div>
            <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">From date</label>
            <input type="date" value={startDate} onChange={(e) => onStartDateChange(e.target.value)} className={inputClassName} />
          </div>
          <div>
            <label className="block text-xs font-medium uppercase tracking-wide text-muted-foreground">To date</label>
            <input type="date" value={endDate} onChange={(e) => onEndDateChange(e.target.value)} className={inputClassName} />
          </div>
        </>
      ) : null}
    </div>
  );
}

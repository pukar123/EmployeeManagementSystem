"use client";

type Option = { id: number; label: string };

type AttendanceReportFiltersProps = {
  employeeId: number | null;
  departmentId: number | null;
  fromDate: string;
  toDate: string;
  groupBy: "week" | "month";
  employees: Option[];
  departments: Option[];
  onEmployeeChange: (value: number | null) => void;
  onDepartmentChange: (value: number | null) => void;
  onFromDateChange: (value: string) => void;
  onToDateChange: (value: string) => void;
  onGroupByChange: (value: "week" | "month") => void;
};

export function AttendanceReportFilters({
  employeeId,
  departmentId,
  fromDate,
  toDate,
  groupBy,
  employees,
  departments,
  onEmployeeChange,
  onDepartmentChange,
  onFromDateChange,
  onToDateChange,
  onGroupByChange,
}: AttendanceReportFiltersProps) {
  return (
    <div className="grid gap-4 rounded-xl border border-border bg-card p-4 sm:grid-cols-2 lg:grid-cols-6 dark:bg-card">
      <label className="block">
        <span className="text-xs font-medium text-muted-foreground">Employee</span>
        <select
          value={employeeId ?? ""}
          onChange={(e) => onEmployeeChange(e.target.value ? Number(e.target.value) : null)}
          className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
        >
          <option value="">All</option>
          {employees.map((item) => (
            <option key={item.id} value={item.id}>
              {item.label}
            </option>
          ))}
        </select>
      </label>

      <label className="block">
        <span className="text-xs font-medium text-muted-foreground">Department</span>
        <select
          value={departmentId ?? ""}
          onChange={(e) => onDepartmentChange(e.target.value ? Number(e.target.value) : null)}
          className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
        >
          <option value="">All</option>
          {departments.map((item) => (
            <option key={item.id} value={item.id}>
              {item.label}
            </option>
          ))}
        </select>
      </label>

      <label className="block">
        <span className="text-xs font-medium text-muted-foreground">From</span>
        <input
          type="date"
          value={fromDate}
          onChange={(e) => onFromDateChange(e.target.value)}
          className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
        />
      </label>

      <label className="block">
        <span className="text-xs font-medium text-muted-foreground">To</span>
        <input
          type="date"
          value={toDate}
          onChange={(e) => onToDateChange(e.target.value)}
          className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
        />
      </label>

      <label className="block">
        <span className="text-xs font-medium text-muted-foreground">Group by</span>
        <select
          value={groupBy}
          onChange={(e) => onGroupByChange(e.target.value as "week" | "month")}
          className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
        >
          <option value="week">Week</option>
          <option value="month">Month</option>
        </select>
      </label>
    </div>
  );
}

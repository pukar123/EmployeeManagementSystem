"use client";

import { useMemo, useState } from "react";
import { useDepartments } from "@/features/departments/hooks";
import { useEmployees } from "@/features/employees/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Spinner } from "@/shared/components/Spinner";
import { useAttendanceAbsenteeism, useAttendancePunctuality } from "../hooks/useAttendanceAnalytics";
import type { AttendanceReportFilter } from "../types/attendance-report.types";
import { AttendanceAnalyticsCards } from "./AttendanceAnalyticsCards";
import { AttendanceReportFilters } from "./AttendanceReportFilters";

function toIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function AttendanceAnalyticsPage() {
  const { organizationId } = useOrganizationContext();
  const { data: employees = [] } = useEmployees();
  const { data: departments = [] } = useDepartments();

  const [employeeId, setEmployeeId] = useState<number | null>(null);
  const [departmentId, setDepartmentId] = useState<number | null>(null);
  const [groupBy, setGroupBy] = useState<"week" | "month">("week");
  const [fromDate, setFromDate] = useState(toIsoDate(new Date(new Date().setDate(new Date().getDate() - 30))));
  const [toDate, setToDate] = useState(toIsoDate(new Date()));

  const filter: AttendanceReportFilter | null = organizationId
    ? {
        organizationId,
        fromDate,
        toDate,
        employeeId: employeeId ?? undefined,
        departmentId: departmentId ?? undefined,
        groupBy,
      }
    : null;

  const punctualityQuery = useAttendancePunctuality(filter);
  const absenteeismQuery = useAttendanceAbsenteeism(filter);

  const employeeOptions = useMemo(
    () => employees.map((x) => ({ id: x.id, label: `${x.firstName} ${x.lastName}` })),
    [employees],
  );
  const departmentOptions = useMemo(() => departments.map((x) => ({ id: x.id, label: x.name })), [departments]);

  if (!organizationId) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (punctualityQuery.isError || absenteeismQuery.isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
        {getErrorMessage(punctualityQuery.error ?? absenteeismQuery.error)}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Attendance analytics</h1>
        <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Late arrivals, early departures, and absenteeism trends.</p>
      </div>

      <AttendanceReportFilters
        employeeId={employeeId}
        departmentId={departmentId}
        fromDate={fromDate}
        toDate={toDate}
        groupBy={groupBy}
        employees={employeeOptions}
        departments={departmentOptions}
        onEmployeeChange={setEmployeeId}
        onDepartmentChange={setDepartmentId}
        onFromDateChange={setFromDate}
        onToDateChange={setToDate}
        onGroupByChange={setGroupBy}
      />

      {punctualityQuery.isLoading || absenteeismQuery.isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      ) : (
        <AttendanceAnalyticsCards punctuality={punctualityQuery.data} absenteeism={absenteeismQuery.data} />
      )}
    </div>
  );
}

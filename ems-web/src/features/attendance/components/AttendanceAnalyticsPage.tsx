"use client";

import { useMemo, useState } from "react";
import { useDepartments } from "@/features/departments/hooks";
import { useEmployees } from "@/features/employees/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Spinner } from "@/shared/components/Spinner";
import { useAttendanceAccess } from "../hooks/useAttendanceAccess";
import { useAttendanceAbsenteeism, useAttendancePunctuality } from "../hooks/useAttendanceAnalytics";
import type { AttendanceReportFilter } from "../types/attendance-report.types";
import { AttendanceAnalyticsCards } from "./AttendanceAnalyticsCards";
import { AttendanceNotLinkedNotice } from "./AttendanceNotLinkedNotice";
import { AttendanceReportFilters } from "./AttendanceReportFilters";

function toIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function AttendanceAnalyticsPage() {
  const { organizationId: currentOrganizationId } = useOrganizationContext();
  const access = useAttendanceAccess();
  const canManageOthers = access.canManageOthers;
  // Employee and department pickers are for attendance admins only; other staff are scoped by the server.
  const { data: employees = [] } = useEmployees(canManageOthers);
  const { data: departments = [] } = useDepartments(canManageOthers);
  // Non-admins may only report on their own organisation (the server refuses any other).
  const organizationId = canManageOthers ? currentOrganizationId : access.linkedOrganizationId;

  const [employeeId, setEmployeeId] = useState<number | null>(null);
  const [departmentId, setDepartmentId] = useState<number | null>(null);
  const [groupBy, setGroupBy] = useState<"week" | "month">("week");
  const [fromDate, setFromDate] = useState(toIsoDate(new Date(new Date().setDate(new Date().getDate() - 30))));
  const [toDate, setToDate] = useState(toIsoDate(new Date()));

  const filter: AttendanceReportFilter | null = organizationId && !access.isLoading
    ? {
        organizationId,
        fromDate,
        toDate,
        employeeId: canManageOthers ? (employeeId ?? undefined) : undefined,
        departmentId: canManageOthers ? (departmentId ?? undefined) : undefined,
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

  if (access.isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
        {getErrorMessage(access.error)}
      </div>
    );
  }

  if (!access.isLoading && !canManageOthers && access.linkedEmployeeId == null) {
    return <AttendanceNotLinkedNotice />;
  }

  if (!organizationId || access.isLoading) {
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
        <h1 className="text-2xl font-semibold text-foreground">Attendance analytics</h1>
        <p className="mt-1 text-sm text-muted-foreground">Late arrivals, early departures, and absenteeism trends.</p>
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
        showPeopleFilters={canManageOthers}
      />

      {canManageOthers ? null : (
        <p className="text-sm text-muted-foreground">
          Showing your own attendance, and your direct reports&apos; if you manage a team.
        </p>
      )}

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

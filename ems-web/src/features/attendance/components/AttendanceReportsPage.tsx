"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useDepartments } from "@/features/departments/hooks";
import { useEmployees } from "@/features/employees/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Spinner } from "@/shared/components/Spinner";
import { useAttendanceExport } from "../hooks/useAttendanceExport";
import { useAttendanceDailyReport, useAttendancePeriodReport } from "../hooks/useAttendanceReport";
import type { AttendanceExportFormat } from "../types/attendance-analytics.types";
import type { AttendanceReportFilter } from "../types/attendance-report.types";
import { AttendanceExportMenu } from "./AttendanceExportMenu";
import { AttendanceReportFilters } from "./AttendanceReportFilters";
import { AttendanceReportTable } from "./AttendanceReportTable";
import { AttendanceTrendChart } from "./AttendanceTrendChart";

function toIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function triggerDownload(blob: Blob, fileName: string): void {
  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  window.URL.revokeObjectURL(url);
}

export function AttendanceReportsPage() {
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

  const dailyQuery = useAttendanceDailyReport(filter);
  const periodQuery = useAttendancePeriodReport(filter);
  const exportMutation = useAttendanceExport();

  const employeeOptions = useMemo(
    () => employees.map((x) => ({ id: x.id, label: `${x.firstName} ${x.lastName}` })),
    [employees],
  );
  const departmentOptions = useMemo(() => departments.map((x) => ({ id: x.id, label: x.name })), [departments]);

  const onExport = async (format: AttendanceExportFormat) => {
    if (!filter) return;
    try {
      const blob = await exportMutation.mutateAsync({ ...filter, format });
      triggerDownload(blob, `attendance-report-${filter.fromDate}-${filter.toDate}.${format}`);
      toast.success(`Report exported as ${format.toUpperCase()}.`);
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  if (!organizationId) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (dailyQuery.isError || periodQuery.isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
        {getErrorMessage(dailyQuery.error ?? periodQuery.error)}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Attendance reports</h1>
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Daily and weekly/monthly summaries with export options.</p>
        </div>
        <AttendanceExportMenu pending={exportMutation.isPending} onExport={(format) => void onExport(format)} />
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

      {dailyQuery.isLoading || periodQuery.isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      ) : (
        <>
          <AttendanceReportTable dailyRows={dailyQuery.data ?? []} periodRows={periodQuery.data ?? []} />
          <AttendanceTrendChart rows={periodQuery.data ?? []} />
        </>
      )}
    </div>
  );
}

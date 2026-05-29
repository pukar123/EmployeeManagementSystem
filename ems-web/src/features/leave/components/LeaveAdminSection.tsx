"use client";

import { useMemo, useState } from "react";
import { useQueries } from "@tanstack/react-query";
import { toast } from "sonner";
import { useEmployees } from "@/features/employees/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { useLeaveAdminSummary, useLeaveMutations, useLeaveTypes } from "../hooks";
import { leaveService } from "../services/leaveService";
import { leaveKeys } from "../services/query-keys";
import type { LeaveAdminSummary, LeaveRequest } from "../types/leave.types";

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function leaveUnitLabel(unit: 1 | 2): string {
  return unit === 2 ? "Hours" : "Days";
}

function buildFallbackSummary(organizationId: number | null, requests: LeaveRequest[]): LeaveAdminSummary {
  const today = new Date().toISOString().slice(0, 10);
  const appliedCount = requests.filter((x) => x.status === "Pending" || x.status === "ModifiedPending").length;
  const approvedCount = requests.filter((x) => x.status === "Approved").length;
  const rejectedCount = requests.filter((x) => x.status === "Rejected").length;
  const cancelledCount = requests.filter((x) => x.status === "Cancelled").length;
  const currentlyOnLeaveCount = requests.filter((x) => {
    if (x.status !== "Approved" && x.status !== "Pending" && x.status !== "ModifiedPending") return false;
    const start = x.startDateUtc.slice(0, 10);
    const end = x.endDateUtc.slice(0, 10);
    return start <= today && end >= today;
  }).length;

  return {
    organizationId: organizationId ?? 0,
    asOfDateUtc: `${today}T00:00:00Z`,
    appliedCount,
    approvedCount,
    rejectedCount,
    cancelledCount,
    currentlyOnLeaveCount,
  };
}

export function LeaveAdminSection() {
  const { organizationId } = useOrganizationContext();
  const employeesQuery = useEmployees();
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [leaveTypeId, setLeaveTypeId] = useState<number | null>(null);
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState(todayIso());
  const [requestedAmount, setRequestedAmount] = useState("1");
  const [reason, setReason] = useState("");
  const leaveTypesQuery = useLeaveTypes(organizationId);
  const adminSummaryQuery = useLeaveAdminSummary(organizationId);
  const employeeId = selectedEmployeeId ?? employeesQuery.data?.[0]?.id ?? null;
  const selectedLeaveTypeId = leaveTypeId ?? leaveTypesQuery.data?.[0]?.id ?? null;
  const selectedLeaveType = useMemo(() => {
    if (selectedLeaveTypeId == null) return null;
    return (leaveTypesQuery.data ?? []).find((x) => x.id === selectedLeaveTypeId) ?? null;
  }, [leaveTypesQuery.data, selectedLeaveTypeId]);
  const leaveTypeUnitById = useMemo(() => new Map((leaveTypesQuery.data ?? []).map((x) => [x.id, x.unit])), [leaveTypesQuery.data]);
  const { createRequest, bulkImport } = useLeaveMutations(employeeId);
  const [importText, setImportText] = useState("");

  const sampleRows = useMemo(() => {
    return importText
      .split("\n")
      .map((x) => x.trim())
      .filter(Boolean)
      .map((line) => {
        const [employeeId, leaveTypeId, startDateUtc, endDateUtc, requestedAmount] = line.split(",");
        const parsedLeaveTypeId = Number(leaveTypeId);
        return {
          employeeId: Number(employeeId),
          leaveTypeId: parsedLeaveTypeId,
          startDateUtc,
          endDateUtc,
          unit: leaveTypeUnitById.get(parsedLeaveTypeId) ?? 1,
          requestedAmount: Number(requestedAmount),
          reason: "Bulk import",
        };
      });
  }, [importText, leaveTypeUnitById]);

  const employeeIds = useMemo(() => (employeesQuery.data ?? []).map((x) => x.id), [employeesQuery.data]);
  const fallbackRequestsQueries = useQueries({
    queries: employeeIds.map((employeeId) => ({
      queryKey: leaveKeys.requests(employeeId),
      queryFn: () => leaveService.getLeaveRequests(employeeId),
      enabled: organizationId != null && adminSummaryQuery.isError,
      staleTime: 30_000,
    })),
  });
  const fallbackSummary = useMemo(() => {
    if (!adminSummaryQuery.isError || organizationId == null) return null;
    if (fallbackRequestsQueries.some((q) => q.isLoading)) return null;
    if (fallbackRequestsQueries.some((q) => q.isError)) return null;
    const allRequests = fallbackRequestsQueries.flatMap((q) => q.data ?? []);
    return buildFallbackSummary(organizationId, allRequests);
  }, [adminSummaryQuery.isError, fallbackRequestsQueries, organizationId]);

  const summary = adminSummaryQuery.data ?? fallbackSummary;
  const usingFallback = !adminSummaryQuery.data && fallbackSummary != null;

  const runImport = async () => {
    if (!organizationId) return;
    try {
      const result = await bulkImport.mutateAsync({
        organizationId,
        importKey: `manual-${Date.now()}`,
        items: sampleRows,
      });
      toast.success(`Import complete: ${result.importedRows}/${result.totalRows} rows.`);
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  const submitManualEntry = async () => {
    if (!employeeId || !selectedLeaveTypeId || !selectedLeaveType) return;
    try {
      await createRequest.mutateAsync({
        employeeId,
        leaveTypeId: selectedLeaveTypeId,
        startDateUtc: startDate,
        endDateUtc: endDate,
        unit: selectedLeaveType.unit,
        requestedAmount: Number(requestedAmount),
        reason: reason.trim() || undefined,
      });
      toast.success("Leave request submitted.");
      setReason("");
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Leave Admin</h1>
        <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Policy and import operations (workflow deferred).</p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
          <p className="text-xs text-zinc-500">Applied</p>
          <p className="text-2xl font-semibold">{summary?.appliedCount ?? 0}</p>
          <p className="mt-1 text-xs text-zinc-500">Pending + modified pending requests</p>
        </div>
        <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
          <p className="text-xs text-zinc-500">Approved</p>
          <p className="text-2xl font-semibold">{summary?.approvedCount ?? 0}</p>
          <p className="mt-1 text-xs text-zinc-500">Separate metric; workflow approval remains deferred</p>
        </div>
        <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
          <p className="text-xs text-zinc-500">On leave today</p>
          <p className="text-2xl font-semibold">{summary?.currentlyOnLeaveCount ?? 0}</p>
          <p className="mt-1 text-xs text-zinc-500">Date-overlap based active leave count</p>
        </div>
      </div>

      {usingFallback ? (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-xs text-amber-900 dark:border-amber-900/40 dark:bg-amber-950/40 dark:text-amber-200">
          Summary API is unavailable, so these metrics are calculated from employee leave requests and may be delayed.
        </div>
      ) : null}

      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <h2 className="text-lg font-medium">Configured leave types</h2>
        <ul className="mt-3 list-disc space-y-1 pl-5 text-sm">
          {(leaveTypesQuery.data ?? []).map((t) => (
            <li key={t.id}>{t.name} ({leaveUnitLabel(t.unit)})</li>
          ))}
        </ul>
      </div>

      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <h2 className="text-lg font-medium">Manual leave entry</h2>
        <p className="mt-1 text-xs text-zinc-500">Create leave for any employee.</p>
        <div className="mt-3 grid gap-4 md:grid-cols-3">
          <label className="block">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Employee</span>
            <select
              className="mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
              value={employeeId ?? ""}
              onChange={(e) => setSelectedEmployeeId(Number(e.target.value))}
            >
              {(employeesQuery.data ?? []).map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.firstName} {employee.lastName}
                </option>
              ))}
            </select>
          </label>
          <label className="block">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Leave type</span>
            <select
              className="mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
              value={selectedLeaveTypeId ?? ""}
              onChange={(e) => setLeaveTypeId(Number(e.target.value))}
            >
              {(leaveTypesQuery.data ?? []).map((leaveType) => (
                <option key={leaveType.id} value={leaveType.id}>
                  {leaveType.name}
                </option>
              ))}
            </select>
          </label>
          <div className="rounded-lg border border-zinc-200 bg-white px-4 py-3 dark:border-zinc-700 dark:bg-zinc-950">
            <p className="text-xs text-zinc-500">Unit</p>
            <p className="text-xl font-semibold">
              {leaveUnitLabel(selectedLeaveType?.unit ?? 1)}
            </p>
          </div>
        </div>

        <div className="mt-4 grid gap-4 md:grid-cols-4">
          <label className="block">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Start date</span>
            <input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              className="mt-1 w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">End date</span>
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className="mt-1 w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Requested amount</span>
            <input
              type="number"
              min="0.5"
              step="0.5"
              value={requestedAmount}
              onChange={(e) => setRequestedAmount(e.target.value)}
              className="mt-1 w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Reason</span>
            <input
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="mt-1 w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
            />
          </label>
        </div>

        <div className="mt-4">
          <Button
            type="button"
            onClick={() => void submitManualEntry()}
            disabled={createRequest.isPending || !employeeId || !selectedLeaveTypeId || !selectedLeaveType}
          >
            Add leave
          </Button>
        </div>
      </div>

      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <h2 className="text-lg font-medium">Bulk import (CSV lines)</h2>
        <p className="mt-1 text-xs text-zinc-500">Format per line: employeeId,leaveTypeId,startDate,endDate,requestedAmount</p>
        <textarea
          value={importText}
          onChange={(e) => setImportText(e.target.value)}
          className="mt-3 min-h-40 w-full rounded-lg border border-zinc-300 px-3 py-2 font-mono text-xs dark:border-zinc-600 dark:bg-zinc-900"
          placeholder="1,2,2026-05-01,2026-05-03,3"
        />
        <div className="mt-3">
          <Button type="button" onClick={() => void runImport()} disabled={bulkImport.isPending || sampleRows.length === 0}>
            Run import
          </Button>
        </div>
      </div>
    </div>
  );
}

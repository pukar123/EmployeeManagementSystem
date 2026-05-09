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
  const leaveTypesQuery = useLeaveTypes(organizationId);
  const adminSummaryQuery = useLeaveAdminSummary(organizationId);
  const { bulkImport } = useLeaveMutations(null);
  const [importText, setImportText] = useState("");

  const sampleRows = useMemo(() => {
    return importText
      .split("\n")
      .map((x) => x.trim())
      .filter(Boolean)
      .map((line) => {
        const [employeeId, leaveTypeId, startDateUtc, endDateUtc, requestedAmount] = line.split(",");
        return {
          employeeId: Number(employeeId),
          leaveTypeId: Number(leaveTypeId),
          startDateUtc,
          endDateUtc,
          unit: "Days" as const,
          requestedAmount: Number(requestedAmount),
          reason: "Bulk import",
        };
      });
  }, [importText]);

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
            <li key={t.id}>{t.name} ({t.unit})</li>
          ))}
        </ul>
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

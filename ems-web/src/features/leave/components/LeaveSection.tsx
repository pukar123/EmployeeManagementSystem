"use client";

import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { useEmployees } from "@/features/employees/hooks";
import { employeePortalKeys } from "@/features/employee-portal/services/query-keys";
import { employeePortalService } from "@/features/employee-portal/services/employeePortalService";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { useLeaveBalances, useLeaveMutations, useLeaveRequests, useLeaveTypes } from "../hooks";

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function leaveUnitLabel(unit: 1 | 2): string {
  return unit === 2 ? "Hours" : "Days";
}

export type LeaveSectionProps = {
  /**
   * When set with `selfServiceOrganizationId`, hides the employee picker and scopes all API calls to this employee (employee portal).
   */
  selfServiceEmployeeId?: number | null;
  selfServiceOrganizationId?: number | null;
  /** Optional heading override (e.g. portal page). */
  title?: string;
};

export function LeaveSection({
  selfServiceEmployeeId,
  selfServiceOrganizationId,
  title = "Leave Management",
}: LeaveSectionProps = {}) {
  const { organizationId } = useOrganizationContext();
  const isPortalSelf =
    selfServiceEmployeeId != null && selfServiceEmployeeId > 0 && selfServiceOrganizationId != null;

  const eligibilityQuery = useQuery({
    queryKey: employeePortalKeys.eligibility(),
    queryFn: () => employeePortalService.getEligibility(),
    enabled: !isPortalSelf,
    staleTime: 0,
    refetchOnMount: "always",
  });

  const canManageOthers =
    !isPortalSelf && (eligibilityQuery.data?.canManageOtherEmployeesLeave ?? false);

  const { data: employees = [], isLoading: employeesLoading } = useEmployees(canManageOthers);

  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [leaveTypeId, setLeaveTypeId] = useState<number | null>(null);
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState(todayIso());
  const [requestedAmount, setRequestedAmount] = useState("1");
  const [reason, setReason] = useState("");

  const effectiveOrgId = isPortalSelf
    ? selfServiceOrganizationId!
    : organizationId ?? eligibilityQuery.data?.linkedOrganizationId ?? null;

  const employeeId = isPortalSelf
    ? selfServiceEmployeeId!
    : canManageOthers
      ? (selectedEmployeeId ?? employees[0]?.id ?? null)
      : (eligibilityQuery.data?.linkedEmployeeId ?? null);

  const leaveTypesQuery = useLeaveTypes(effectiveOrgId);
  const balancesQuery = useLeaveBalances(employeeId);
  const requestsQuery = useLeaveRequests(employeeId);
  const { createRequest, cancelRequest } = useLeaveMutations(employeeId);

  const selectedLeaveTypeId = leaveTypeId ?? leaveTypesQuery.data?.[0]?.id ?? null;
  const selectedLeaveType = useMemo(() => {
    if (selectedLeaveTypeId == null) return null;
    return leaveTypesQuery.data?.find((x) => x.id === selectedLeaveTypeId) ?? null;
  }, [leaveTypesQuery.data, selectedLeaveTypeId]);
  const selectedBalance = useMemo(() => {
    if (selectedLeaveTypeId == null) return null;
    return balancesQuery.data?.find((x) => x.leaveTypeId === selectedLeaveTypeId) ?? null;
  }, [balancesQuery.data, selectedLeaveTypeId]);

  const submit = async () => {
    if (!employeeId || !selectedLeaveTypeId || !selectedLeaveType) return;
    try {
      await createRequest.mutateAsync({
        employeeId,
        leaveTypeId: selectedLeaveTypeId,
        startDateUtc: startDate,
        endDateUtc: endDate,
        unit: selectedLeaveType?.unit ?? 1,
        requestedAmount: Number(requestedAmount),
        reason: reason.trim() || undefined,
      });
      toast.success("Leave request submitted.");
      setReason("");
    } catch (e) {
      toast.error(getErrorMessage(e));
    }
  };

  const loadingEligibility = !isPortalSelf && eligibilityQuery.isLoading;
  const loadingEmployees = canManageOthers && employeesLoading;
  const loadingLeaveTypes = leaveTypesQuery.isLoading;

  if (loadingEligibility || loadingEmployees || loadingLeaveTypes) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (!isPortalSelf && !canManageOthers && !eligibilityQuery.data?.linkedEmployeeId) {
    return (
      <div className="rounded-xl border border-border bg-muted/30 px-4 py-6 text-sm text-muted-foreground" role="status">
        <p className="font-medium text-foreground">Leave</p>
        <p className="mt-2">
          Your account is not linked to an employee record, or you do not have permission to manage leave for others.
          Use the employee portal after your login is linked to an employee.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">{title}</h1>
        <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Self-service leave requests (workflow deferred).</p>
      </div>

      <div className={`grid gap-4 ${isPortalSelf || !canManageOthers ? "md:grid-cols-2" : "md:grid-cols-3"}`}>
        {canManageOthers ? (
          <label className="block">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Employee</span>
            <select
              className="mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
              value={employeeId ?? ""}
              onChange={(e) => setSelectedEmployeeId(Number(e.target.value))}
            >
              {employees.map((e) => (
                <option key={e.id} value={e.id}>
                  {e.firstName} {e.lastName}
                </option>
              ))}
            </select>
          </label>
        ) : null}
        <label className="block">
          <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">Leave type</span>
          <select
            className="mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-600 dark:bg-zinc-900"
            value={selectedLeaveTypeId ?? ""}
            onChange={(e) => setLeaveTypeId(Number(e.target.value))}
          >
            {leaveTypesQuery.data?.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </label>
        <div className="rounded-lg border border-zinc-200 bg-white px-4 py-3 dark:border-zinc-700 dark:bg-zinc-950">
          <p className="text-xs text-zinc-500">Available balance</p>
          <p className="text-xl font-semibold">{selectedBalance?.availableAmount ?? 0}</p>
          <p className="mt-1 text-xs text-zinc-500">{leaveUnitLabel(selectedLeaveType?.unit ?? 1)}</p>
        </div>
      </div>

      <div className="grid gap-4 rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950 md:grid-cols-4">
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
        <div className="md:col-span-4">
          <Button
            type="button"
            onClick={() => void submit()}
            disabled={createRequest.isPending || !employeeId || !selectedLeaveTypeId || !selectedLeaveType}
          >
            Submit request
          </Button>
        </div>
      </div>

      <div className="rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-700 dark:bg-zinc-950">
        <h2 className="text-lg font-medium">Requests</h2>
        <div className="mt-3 overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="text-left text-zinc-500">
                <th className="pb-2 pr-3">Date range</th>
                <th className="pb-2 pr-3">Amount</th>
                <th className="pb-2 pr-3">Status</th>
                <th className="pb-2 pr-3">Action</th>
              </tr>
            </thead>
            <tbody>
              {(requestsQuery.data ?? []).map((row) => (
                <tr key={row.id} className="border-t border-zinc-200 dark:border-zinc-800">
                  <td className="py-2 pr-3">
                    {row.startDateUtc.slice(0, 10)} to {row.endDateUtc.slice(0, 10)}
                  </td>
                  <td className="py-2 pr-3">{row.requestedAmount}</td>
                  <td className="py-2 pr-3">{row.status}</td>
                  <td className="py-2 pr-3">
                    <Button
                      type="button"
                      variant="secondary"
                      disabled={cancelRequest.isPending || row.status === "Cancelled"}
                      onClick={() =>
                        void cancelRequest.mutateAsync(row.id).catch((e) => toast.error(getErrorMessage(e)))
                      }
                    >
                      Cancel
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

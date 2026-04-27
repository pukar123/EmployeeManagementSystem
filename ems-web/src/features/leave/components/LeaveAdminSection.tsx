"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { useLeaveMutations, useLeaveTypes } from "../hooks";

export function LeaveAdminSection() {
  const { organizationId } = useOrganizationContext();
  const leaveTypesQuery = useLeaveTypes(organizationId);
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

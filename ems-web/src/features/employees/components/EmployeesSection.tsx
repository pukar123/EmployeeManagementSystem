"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useCreateActionParam } from "@/features/command-palette/hooks/useCreateActionParam";
import { useDepartments } from "@/features/departments/hooks";
import { useJobPositions } from "@/features/job-positions/hooks";
import { useSites } from "@/features/sites/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { PageHeader } from "@/shared/components/PageHeader";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { cn } from "@/shared/utils/cn";
import { useEmployeeDirectory } from "../hooks";
import { useEmployeeCapabilities } from "../hooks/useEmployeeCapabilities";
import { employeeService } from "../services/employeeService";
import { employeeKeys } from "../services/query-keys";
import type { EmployeeDirectoryItem, EmployeeDirectoryQuery } from "../types/employee.types";
import { defaultDirectoryQuery, EmployeeDirectoryFilters } from "./EmployeeDirectoryFilters";
import { EmployeeTable } from "./EmployeeTable";
import { CreateEmployeeWizard } from "./CreateEmployeeWizard";

export function EmployeesSection() {
  const queryClient = useQueryClient();
  const { organizationId } = useOrganizationContext();
  const { capabilities, isLoading: capabilitiesLoading } = useEmployeeCapabilities();
  const [filterState, setFilterState] = useState<EmployeeDirectoryQuery | null>(null);

  useEffect(() => {
    if (organizationId) {
      setFilterState((prev) => prev ?? defaultDirectoryQuery(organizationId));
    }
  }, [organizationId]);

  const directoryQuery = useMemo(() => {
    if (!organizationId) return null;
    const base = filterState ?? defaultDirectoryQuery(organizationId);
    return { ...base, organizationId };
  }, [organizationId, filterState]);
  const [createOpen, setCreateOpen] = useState(false);
  const createCloseGuardRef = useRef<(() => boolean) | null>(null);

  useCreateActionParam(() => setCreateOpen(true), capabilities.manage);
  const [exportBusy, setExportBusy] = useState(false);
  const [restoreBusy, setRestoreBusy] = useState(false);

  const { data: departments = [] } = useDepartments();
  const { data: jobPositions = [] } = useJobPositions(organizationId);
  const { data: sites = [] } = useSites();

  const managerPickerQuery = useMemo<EmployeeDirectoryQuery | null>(
    () =>
      organizationId
        ? {
            ...defaultDirectoryQuery(organizationId),
            page: 1,
            pageSize: 100,
            employmentStatus: 0,
          }
        : null,
    [organizationId],
  );
  const managersQuery = useEmployeeDirectory(managerPickerQuery);
  const managers = managersQuery.data?.items ?? [];

  const { data, isLoading, isError, error, refetch, isFetching } = useEmployeeDirectory(directoryQuery);

  const updateQuery = (patch: Partial<EmployeeDirectoryQuery>) => {
    setFilterState((prev) => {
      if (!organizationId) return prev;
      const base = prev ?? defaultDirectoryQuery(organizationId);
      return { ...base, ...patch };
    });
  };

  const handleSort = (sortBy: string) => {
    const current = directoryQuery?.sortBy ?? "name";
    const currentDir = directoryQuery?.sortDirection ?? "asc";
    const nextDir = current === sortBy && currentDir === "asc" ? "desc" : "asc";
    updateQuery({ sortBy, sortDirection: nextDir, page: 1 });
  };

  const clearFilters = () => {
    if (!organizationId) return;
    setFilterState({
      ...defaultDirectoryQuery(organizationId),
      isArchived: filterState?.isArchived ?? false,
    });
  };

  const invalidateDirectory = () => {
    void queryClient.invalidateQueries({ queryKey: employeeKeys.all });
  };

  const handleExport = async () => {
    if (!directoryQuery) return;
    setExportBusy(true);
    try {
      const blob = await employeeService.exportEmployeeDirectoryCsv(directoryQuery);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `employees-${new Date().toISOString().slice(0, 10)}.csv`;
      anchor.click();
      URL.revokeObjectURL(url);
      toast.success("Employee directory exported.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setExportBusy(false);
    }
  };

  const handleRestore = async (employee: EmployeeDirectoryItem) => {
    if (!window.confirm(`Restore ${employee.firstName} ${employee.lastName} (${employee.employeeNumber})?`)) return;
    setRestoreBusy(true);
    try {
      await employeeService.restoreEmployee(employee.id);
      toast.success("Employee restored.");
      invalidateDirectory();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setRestoreBusy(false);
    }
  };

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const page = data?.page ?? 1;
  const totalPages = data?.totalPages ?? 1;
  const isArchiveView = directoryQuery?.isArchived ?? false;

  if (capabilitiesLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (!capabilities.view) {
    return (
      <div className="rounded-xl border border-border bg-card p-8 text-center">
        <p className="text-sm text-muted-foreground">You do not have permission to view employees.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={isArchiveView ? "Employee archive" : "Employees"}
        description="Search, filter, and open employee profiles. Employment lifecycle actions live on each profile."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => updateQuery({ isArchived: !isArchiveView, page: 1 })}
            >
              {isArchiveView ? "Active directory" : "Archive view"}
            </Button>
            <Button type="button" variant="secondary" disabled={exportBusy || isLoading || !capabilities.export} onClick={() => void handleExport()}>
              {exportBusy ? "Exporting…" : "Export CSV"}
            </Button>
            {!isArchiveView && capabilities.manage ? (
              <Button type="button" onClick={() => setCreateOpen(true)}>
                Add employee
              </Button>
            ) : null}
          </div>
        }
      />

      {directoryQuery ? (
        <EmployeeDirectoryFilters
          query={directoryQuery}
          onChange={updateQuery}
          onClearAll={clearFilters}
          departments={departments.filter((d) => d.organizationId === organizationId)}
          jobPositions={jobPositions}
          managers={managers}
          sites={sites}
          totalCount={totalCount}
        />
      ) : null}

      {isLoading ? (
        <div className="flex flex-col items-center justify-center gap-2 py-16">
          <Spinner />
          <p className="text-sm text-muted-foreground">Loading employees…</p>
        </div>
      ) : isError ? (
        <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-6 text-center">
          <p className="text-sm text-destructive">Could not load the employee directory.</p>
          <p className="mt-1 text-xs text-muted-foreground">{getErrorMessage(error)}</p>
          <Button type="button" className="mt-4" variant="secondary" onClick={() => void refetch()}>
            Retry
          </Button>
        </div>
      ) : totalCount === 0 && !isArchiveView && !directoryQuery?.search && !directoryQuery?.departmentId ? (
        <div className="rounded-xl border border-dashed border-border p-10 text-center">
          <p className="text-lg font-medium">No employees yet</p>
          <p className="mt-1 text-sm text-muted-foreground">Add your first employee to start building the directory.</p>
          <Button type="button" className="mt-4" onClick={() => setCreateOpen(true)} disabled={!capabilities.manage}>
            Add employee
          </Button>
        </div>
      ) : (
        <>
          <div className={cn(isFetching && "opacity-70 transition-opacity")}>
            <EmployeeTable
              employees={items}
              isArchiveView={isArchiveView}
              onRestore={isArchiveView && capabilities.manage ? handleRestore : undefined}
              restoreBusy={restoreBusy}
              sortBy={directoryQuery?.sortBy ?? "name"}
              sortDirection={directoryQuery?.sortDirection ?? "asc"}
              onSort={handleSort}
            />
          </div>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
              <label className="flex items-center gap-2">
                <span>Rows per page</span>
                <select
                  className="rounded-lg border border-input bg-background px-2 py-1 text-sm"
                  value={directoryQuery?.pageSize ?? 25}
                  onChange={(e) => updateQuery({ pageSize: Number(e.target.value), page: 1 })}
                >
                  {[10, 25, 50, 100].map((size) => (
                    <option key={size} value={size}>
                      {size}
                    </option>
                  ))}
                </select>
              </label>
              {totalPages > 1 ? (
                <p>
                  Page {page} of {totalPages}
                </p>
              ) : null}
            </div>
            {totalPages > 1 ? (
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => updateQuery({ page: page - 1 })}
                >
                  Previous
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => updateQuery({ page: page + 1 })}
                >
                  Next
                </Button>
              </div>
            ) : null}
          </div>
        </>
      )}

      <Modal
        open={createOpen}
        title="Add employee"
        onClose={() => setCreateOpen(false)}
        onRequestClose={() => createCloseGuardRef.current?.() ?? true}
        className="max-w-3xl"
      >
        <CreateEmployeeWizard
          onClose={() => setCreateOpen(false)}
          onCreated={() => {
            invalidateDirectory();
          }}
          onRegisterCloseGuard={(guard) => {
            createCloseGuardRef.current = guard;
          }}
        />
      </Modal>
    </div>
  );
}

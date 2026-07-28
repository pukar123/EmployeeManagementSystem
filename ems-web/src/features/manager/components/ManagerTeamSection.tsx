"use client";

import { useEffect, useMemo } from "react";
import { useDepartments } from "@/features/departments/hooks";
import { defaultDirectoryQuery } from "@/features/employees/components/EmployeeDirectoryFilters";
import { useEmployeeDirectory } from "@/features/employees/hooks";
import { useEmployeeCapabilities } from "@/features/employees/hooks/useEmployeeCapabilities";
import { EmploymentStatus } from "@/features/employees/types/employment-status";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { PageHeader } from "@/shared/components/PageHeader";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { ManagerTeamFilters } from "./ManagerTeamFilters";
import { ManagerTeamQuickLinks, ManagerTeamSummaryCards } from "./ManagerTeamSummaryCards";
import { ManagerTeamTable } from "./ManagerTeamTable";
import { useManagerTeamController } from "../hooks";

const selectClass =
  "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20";

export function ManagerTeamSection() {
  const { organizationId } = useOrganizationContext();
  const { capabilities, isLoading: capabilitiesLoading } = useEmployeeCapabilities();
  const { data: departments = [] } = useDepartments();

  const isLikelyAdmin = Boolean(
    capabilities?.view && capabilities?.manage && capabilities?.access && capabilities?.export,
  );

  const {
    query,
    dashboardQuery,
    selectedManagerId,
    setSelectedManagerId,
    adminMode,
    setAdminMode,
    updateQuery,
    resetFilters,
  } = useManagerTeamController();

  useEffect(() => {
    if (!capabilitiesLoading) {
      setAdminMode(isLikelyAdmin);
    }
  }, [capabilitiesLoading, isLikelyAdmin, setAdminMode]);

  const managerPickerQuery = useMemo(
    () =>
      organizationId
        ? {
            ...defaultDirectoryQuery(organizationId),
            page: 1,
            pageSize: 100,
            employmentStatus: EmploymentStatus.Active,
          }
        : null,
    [organizationId],
  );
  const managersQuery = useEmployeeDirectory(managerPickerQuery);
  const managers = managersQuery.data?.items ?? [];

  const { data, isLoading, isError, error, isFetching, refetch } = dashboardQuery;

  useEffect(() => {
    if (data?.allowsManagerSelection) {
      setAdminMode(true);
    }
  }, [data?.allowsManagerSelection, setAdminMode]);

  const handleSort = (sortBy: string) => {
    const current = query?.sortBy ?? "name";
    const currentDir = query?.sortDirection ?? "asc";
    const nextDir = current === sortBy && currentDir === "asc" ? "desc" : "asc";
    updateQuery({ sortBy, sortDirection: nextDir, page: 1 });
  };

  if (!organizationId) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-16">
        <Spinner />
        <p className="text-sm text-muted-foreground">Loading organization…</p>
      </div>
    );
  }

  if (capabilitiesLoading) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-16">
        <Spinner />
        <p className="text-sm text-muted-foreground">Checking access…</p>
      </div>
    );
  }

  const awaitingAdminSelection = adminMode && selectedManagerId == null;

  if (isError && !awaitingAdminSelection) {
    const status = (error as { response?: { status?: number } })?.response?.status;
    if (status === 403) {
      return (
        <div className="rounded-xl border border-border bg-card p-8 text-center shadow-sm">
          <h2 className="text-lg font-semibold text-foreground">Access denied</h2>
          <p className="mt-2 text-sm text-muted-foreground">
            You need the <strong>My team</strong> menu permission and a linked employee profile to view this dashboard.
            Ask your administrator to grant access.
          </p>
        </div>
      );
    }

    return (
      <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-6">
        <p className="text-sm text-destructive">{getErrorMessage(error)}</p>
        <Button type="button" variant="secondary" size="sm" className="mt-3" onClick={() => refetch()}>
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="My team"
        description="Operational view of your direct reports — attendance, leave, tasks, and upcoming changes."
      />

      {adminMode ? (
        <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
          <label className="mb-1 block text-xs font-medium uppercase tracking-wide text-muted-foreground">
            View team for manager
          </label>
          <select
            className={selectClass}
            value={selectedManagerId ?? ""}
            onChange={(e) => {
              const value = e.target.value;
              setSelectedManagerId(value === "" ? null : Number(value));
              updateQuery({ page: 1 });
            }}
          >
            <option value="">Select a manager…</option>
            {managers.map((m) => (
              <option key={m.id} value={m.id}>
                {m.firstName} {m.lastName} ({m.employeeNumber})
              </option>
            ))}
          </select>
        </div>
      ) : null}

      {awaitingAdminSelection ? (
        <div className="rounded-xl border border-dashed border-border bg-muted/30 p-8 text-center">
          <p className="text-sm text-muted-foreground">Select a manager above to load their team dashboard.</p>
        </div>
      ) : (
        <>
          {data ? (
            <p className="text-sm text-muted-foreground">
              Showing direct reports for{" "}
              <span className="font-medium text-foreground">
                {data.managerName} ({data.managerEmployeeNumber})
              </span>
            </p>
          ) : null}

          <ManagerTeamSummaryCards summary={data?.summary} loading={isLoading || isFetching} />
          <ManagerTeamQuickLinks />

          {query ? (
            <ManagerTeamFilters
              query={query}
              onChange={updateQuery}
              onClearAll={resetFilters}
              departments={departments}
              totalCount={data?.totalCount ?? 0}
            />
          ) : null}

          {isLoading ? (
            <div className="flex flex-col items-center justify-center gap-3 py-12">
              <Spinner />
              <p className="text-sm text-muted-foreground">Loading team…</p>
            </div>
          ) : (
            <ManagerTeamTable
              members={data?.items ?? []}
              sortBy={query?.sortBy}
              sortDirection={query?.sortDirection}
              onSort={handleSort}
            />
          )}

          {data && data.totalPages > 1 ? (
            <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-card px-4 py-3">
              <p className="text-sm text-muted-foreground">
                Page {data.page} of {data.totalPages}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={data.page <= 1 || isFetching}
                  onClick={() => updateQuery({ page: Math.max(1, (query?.page ?? 1) - 1) })}
                >
                  Previous
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={data.page >= data.totalPages || isFetching}
                  onClick={() => updateQuery({ page: (query?.page ?? 1) + 1 })}
                >
                  Next
                </Button>
              </div>
            </div>
          ) : null}
        </>
      )}
    </div>
  );
}

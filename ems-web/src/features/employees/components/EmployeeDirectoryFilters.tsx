"use client";

import { useEffect, useMemo, useState } from "react";
import type { Department } from "@/features/departments/types/department.types";
import type { JobPosition } from "@/features/job-positions/types/job-position.types";
import type { Site } from "@/features/sites/types/site.types";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";
import type { EmployeeDirectoryItem, EmployeeDirectoryQuery } from "../types/employee.types";
import { employmentStatusLabels, type EmploymentStatusValue } from "../types/employment-status";
import { Button } from "@/shared/components/Button";
import { SearchInput } from "@/shared/components/SearchInput";

type EmployeeDirectoryFiltersProps = {
  query: EmployeeDirectoryQuery;
  onChange: (patch: Partial<EmployeeDirectoryQuery>) => void;
  onClearAll: () => void;
  departments: Department[];
  jobPositions: JobPosition[];
  managers: EmployeeDirectoryItem[];
  sites: Site[];
  totalCount: number;
};

const selectClass =
  "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20";

export function EmployeeDirectoryFilters({
  query,
  onChange,
  onClearAll,
  departments,
  jobPositions,
  managers,
  sites,
  totalCount,
}: EmployeeDirectoryFiltersProps) {
  const [searchText, setSearchText] = useState(query.search ?? "");
  const [prevQuerySearch, setPrevQuerySearch] = useState(query.search);
  if (query.search !== prevQuerySearch) {
    setPrevQuerySearch(query.search);
    setSearchText(query.search ?? "");
  }

  const debouncedSearch = useDebouncedValue(searchText, 300);

  useEffect(() => {
    if (debouncedSearch !== (query.search ?? "")) {
      onChange({ search: debouncedSearch, page: 1 });
    }
  }, [debouncedSearch, onChange, query.search]);

  const activeFilters = useMemo(() => {
    const chips: string[] = [];
    if (query.search?.trim()) chips.push(`Search: ${query.search.trim()}`);
    if (query.employmentStatus != null) chips.push(`Status: ${employmentStatusLabels[query.employmentStatus]}`);
    if (query.departmentId != null) {
      const d = departments.find((x) => x.id === query.departmentId);
      chips.push(`Department: ${d?.name ?? query.departmentId}`);
    }
    if (query.jobPositionId != null) {
      const p = jobPositions.find((x) => x.id === query.jobPositionId);
      chips.push(`Position: ${p?.title ?? query.jobPositionId}`);
    }
    if (query.managerId != null) {
      const m = managers.find((x) => x.id === query.managerId);
      chips.push(`Manager: ${m ? `${m.firstName} ${m.lastName}` : query.managerId}`);
    }
    if (query.siteId != null) {
      const s = sites.find((x) => x.siteId === query.siteId);
      chips.push(`Site: ${s?.siteName ?? query.siteId}`);
    }
    if (query.loginLinkStatus) chips.push(`Login: ${query.loginLinkStatus.replace("_", " ")}`);
    if (query.isArchived) chips.push("Archive view");
    return chips;
  }, [query, departments, jobPositions, managers, sites]);

  return (
    <div className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-end">
        <div className="flex-1">
          <label className="mb-1 block text-xs font-medium uppercase tracking-wide text-muted-foreground">Search</label>
          <SearchInput
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Name, email, employee number, phone…"
          />
        </div>
        <div className="grid flex-1 grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <FilterSelect
            label="Employment status"
            value={query.employmentStatus ?? ""}
            onChange={(v) =>
              onChange({
                employmentStatus: v === "" ? null : (Number(v) as EmploymentStatusValue),
                page: 1,
              })
            }
            options={[
              { value: "", label: "All statuses" },
              ...Object.entries(employmentStatusLabels).map(([value, label]) => ({ value, label })),
            ]}
          />
          <FilterSelect
            label="Department"
            value={query.departmentId ?? ""}
            onChange={(v) => onChange({ departmentId: v === "" ? null : Number(v), page: 1 })}
            options={[
              { value: "", label: "All departments" },
              ...departments.map((d) => ({ value: String(d.id), label: d.name })),
            ]}
          />
          <FilterSelect
            label="Position"
            value={query.jobPositionId ?? ""}
            onChange={(v) => onChange({ jobPositionId: v === "" ? null : Number(v), page: 1 })}
            options={[
              { value: "", label: "All positions" },
              ...jobPositions.map((p) => ({ value: String(p.id), label: p.title })),
            ]}
          />
          <FilterSelect
            label="Manager"
            value={query.managerId ?? ""}
            onChange={(v) => onChange({ managerId: v === "" ? null : Number(v), page: 1 })}
            options={[
              { value: "", label: "All managers" },
              ...managers.map((m) => ({
                value: String(m.id),
                label: `${m.firstName} ${m.lastName} (${m.employeeNumber})`,
              })),
            ]}
          />
          <FilterSelect
            label="Site"
            value={query.siteId ?? ""}
            onChange={(v) => onChange({ siteId: v === "" ? null : Number(v), page: 1 })}
            options={[
              { value: "", label: "All sites" },
              ...sites.map((s) => ({ value: String(s.siteId), label: s.siteName })),
            ]}
          />
          <FilterSelect
            label="Login link"
            value={query.loginLinkStatus ?? ""}
            onChange={(v) =>
              onChange({
                loginLinkStatus: v === "" ? null : (v as EmployeeDirectoryQuery["loginLinkStatus"]),
                page: 1,
              })
            }
            options={[
              { value: "", label: "Any login status" },
              { value: "linked", label: "Linked (active)" },
              { value: "not_linked", label: "Not linked" },
              { value: "disabled", label: "Linked (disabled)" },
            ]}
          />
        </div>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
        <p className="text-muted-foreground">
          {totalCount === 1 ? "1 employee" : `${totalCount} employees`}
          {query.isArchived ? " in archive" : ""}
        </p>
        {activeFilters.length > 0 ? (
          <div className="flex flex-wrap items-center gap-2">
            {activeFilters.map((chip) => (
              <span key={chip} className="rounded-full bg-muted px-2.5 py-0.5 text-xs text-foreground">
                {chip}
              </span>
            ))}
            <Button type="button" variant="ghost" size="sm" onClick={onClearAll}>
              Clear all filters
            </Button>
          </div>
        ) : null}
      </div>
    </div>
  );
}

function FilterSelect({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string | number;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
}) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</label>
      <select className={selectClass} value={value} onChange={(e) => onChange(e.target.value)}>
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </div>
  );
}

export const defaultDirectoryQuery = (organizationId: number): EmployeeDirectoryQuery => ({
  organizationId,
  page: 1,
  pageSize: 25,
  isArchived: false,
  sortBy: "name",
  sortDirection: "asc",
  employmentStatus: null,
  departmentId: null,
  jobPositionId: null,
  managerId: null,
  siteId: null,
  loginLinkStatus: null,
  search: "",
});

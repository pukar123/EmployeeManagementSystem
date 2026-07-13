"use client";

import { useEffect, useMemo, useState } from "react";
import type { Department } from "@/features/departments/types/department.types";
import { employmentStatusLabels, type EmploymentStatusValue } from "@/features/employees/types/employment-status";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";
import { Button } from "@/shared/components/Button";
import { SearchInput } from "@/shared/components/SearchInput";
import type { ManagerTeamQuery } from "../types/manager-team.types";

type ManagerTeamFiltersProps = {
  query: ManagerTeamQuery;
  onChange: (patch: Partial<ManagerTeamQuery>) => void;
  onClearAll: () => void;
  departments: Department[];
  totalCount: number;
};

const selectClass =
  "w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20";

export function ManagerTeamFilters({
  query,
  onChange,
  onClearAll,
  departments,
  totalCount,
}: ManagerTeamFiltersProps) {
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
    return chips;
  }, [query, departments]);

  return (
    <div className="space-y-4 rounded-xl border border-border bg-card p-4 shadow-sm">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-end">
        <div className="flex-1">
          <label className="mb-1 block text-xs font-medium uppercase tracking-wide text-muted-foreground">Search</label>
          <SearchInput
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Name, email, employee number…"
          />
        </div>
        <div className="grid flex-1 grid-cols-1 gap-3 sm:grid-cols-2">
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
        </div>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border pt-3">
        <p className="text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{totalCount}</span> direct report
          {totalCount === 1 ? "" : "s"}
        </p>
        {activeFilters.length > 0 ? (
          <div className="flex flex-wrap items-center gap-2">
            {activeFilters.map((chip) => (
              <span
                key={chip}
                className="rounded-full border border-border bg-muted/50 px-2.5 py-0.5 text-xs text-muted-foreground"
              >
                {chip}
              </span>
            ))}
            <Button type="button" variant="ghost" size="sm" onClick={onClearAll}>
              Clear filters
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
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    </div>
  );
}

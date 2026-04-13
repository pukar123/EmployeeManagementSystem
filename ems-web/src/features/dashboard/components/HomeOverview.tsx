"use client";

import Link from "next/link";
import { Building2, Briefcase, Users } from "lucide-react";
import { useDepartments } from "@/features/departments/hooks";
import { useEmployees } from "@/features/employees/hooks";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

function StatCard({
  href,
  title,
  value,
  icon: Icon,
  loading,
}: {
  href: string;
  title: string;
  value: number;
  icon: typeof Users;
  loading: boolean;
}) {
  return (
    <Link
      href={href}
      className={cn(
        "group flex flex-col rounded-xl border-2 border-primary/15 bg-card p-6 shadow-sm transition",
        "hover:border-primary/35 hover:shadow-md dark:border-primary/25 dark:hover:border-primary/45",
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="rounded-lg bg-primary/10 p-2.5 text-primary dark:bg-primary/20">
          <Icon className="size-6" aria-hidden />
        </div>
      </div>
      <p className="mt-4 text-sm font-medium text-muted-foreground">{title}</p>
      {loading ? (
        <Skeleton className="mt-2 h-10 w-24" />
      ) : (
        <p className="mt-1 text-4xl font-semibold tabular-nums tracking-tight text-foreground">{value}</p>
      )}
      <span className="mt-3 text-xs font-medium text-primary group-hover:underline">View all →</span>
    </Link>
  );
}

export function HomeOverview() {
  const employeesQuery = useEmployees();
  const departmentsQuery = useDepartments();

  const loading = employeesQuery.isLoading || departmentsQuery.isLoading;
  const error = employeesQuery.isError || departmentsQuery.isError;

  const empCount = employeesQuery.data?.length ?? 0;
  const deptCount = departmentsQuery.data?.length ?? 0;

  if (error) {
    return (
      <div
        className="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive"
        role="alert"
      >
        Could not load dashboard statistics. Open Employees or Departments from the menu, or try refreshing the page.
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="grid gap-4 sm:grid-cols-2">
        <StatCard
          href="/employees"
          title="Total employees"
          value={empCount}
          icon={Users}
          loading={loading}
        />
        <StatCard
          href="/departments"
          title="Departments"
          value={deptCount}
          icon={Building2}
          loading={loading}
        />
      </div>

      <div>
        <h2 className="mb-3 text-sm font-medium uppercase tracking-wide text-muted-foreground">Quick link</h2>
        <Link
          href="/positions"
          className={cn(
            "flex items-center gap-3 rounded-xl border border-border bg-card px-4 py-3 text-sm font-medium shadow-sm transition",
            "hover:border-primary/30 hover:bg-accent/50",
          )}
        >
          <span className="rounded-md bg-primary/10 p-2 text-primary">
            <Briefcase className="size-5" aria-hidden />
          </span>
          <span>Job positions</span>
          <span className="ml-auto text-xs text-muted-foreground">Open →</span>
        </Link>
      </div>
    </div>
  );
}

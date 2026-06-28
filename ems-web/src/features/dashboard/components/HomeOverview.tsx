"use client";

import Link from "next/link";
import {
  ArrowUpRight,
  Briefcase,
  Building2,
  CalendarDays,
  Clock3,
  ListTodo,
  Users,
} from "lucide-react";
import { useDepartments } from "@/features/departments/hooks";
import { useEmployees } from "@/features/employees/hooks";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

const gradientTiles = [
  "from-indigo-500 to-violet-600",
  "from-violet-500 to-purple-600",
  "from-blue-500 to-indigo-600",
  "from-fuchsia-500 to-violet-600",
] as const;

function StatCard({
  href,
  title,
  value,
  icon: Icon,
  loading,
  gradientIndex,
}: {
  href: string;
  title: string;
  value: number;
  icon: typeof Users;
  loading: boolean;
  gradientIndex: number;
}) {
  const gradient = gradientTiles[gradientIndex % gradientTiles.length];

  return (
    <Link
      href={href}
      className={cn(
        "group relative flex flex-col overflow-hidden rounded-2xl border border-border bg-card p-6 shadow-soft transition-all duration-300",
        "hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-soft-lg",
      )}
    >
      <div
        className={cn(
          "mb-4 inline-flex w-fit rounded-xl bg-gradient-to-br p-3 text-white shadow-brand-glow",
          gradient,
        )}
      >
        <Icon className="size-6" aria-hidden />
      </div>
      <p className="text-sm font-medium text-muted-foreground">{title}</p>
      {loading ? (
        <Skeleton className="mt-2 h-12 w-28" />
      ) : (
        <p className="mt-1 text-5xl font-bold tabular-nums tracking-tight text-foreground">{value}</p>
      )}
      <span className="mt-4 inline-flex items-center gap-1 text-xs font-semibold text-primary">
        View all
        <ArrowUpRight className="size-3.5 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
      </span>
    </Link>
  );
}

const quickLinks = [
  { href: "/positions", label: "Job positions", icon: Briefcase },
  { href: "/tasks", label: "Tasks", icon: ListTodo },
  { href: "/attendance", label: "Attendance", icon: Clock3 },
  { href: "/leave", label: "Leave", icon: CalendarDays },
] as const;

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
        className="rounded-2xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive"
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
          gradientIndex={0}
        />
        <StatCard
          href="/departments"
          title="Departments"
          value={deptCount}
          icon={Building2}
          loading={loading}
          gradientIndex={1}
        />
      </div>

      <div>
        <h2 className="mb-4 text-sm font-semibold uppercase tracking-widest text-muted-foreground">
          Quick links
        </h2>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {quickLinks.map(({ href, label, icon: Icon }) => (
            <Link
              key={href}
              href={href}
              className={cn(
                "group flex items-center gap-3 rounded-2xl border border-border bg-card px-4 py-4 shadow-soft transition-all",
                "hover:border-primary/25 hover:bg-accent/40 hover:shadow-soft-lg",
              )}
            >
              <span className="rounded-xl bg-brand-gradient-subtle p-2.5 text-primary">
                <Icon className="size-5" aria-hidden />
              </span>
              <span className="text-sm font-semibold text-foreground">{label}</span>
              <ArrowUpRight className="ml-auto size-4 text-muted-foreground transition-transform group-hover:text-primary group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}

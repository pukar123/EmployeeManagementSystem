"use client";

import Link from "next/link";
import {
  ArrowUpRight,
  CalendarClock,
  CalendarDays,
  Clock3,
  ListTodo,
  UserCheck,
  Users,
} from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import type { ManagerTeamSummary } from "../types/manager-team.types";

const gradientTiles = [
  "from-indigo-500 to-violet-600",
  "from-violet-500 to-purple-600",
  "from-blue-500 to-indigo-600",
  "from-fuchsia-500 to-violet-600",
  "from-emerald-500 to-teal-600",
] as const;

function StatCard({
  title,
  value,
  subtitle,
  icon: Icon,
  loading,
  gradientIndex,
  href,
}: {
  title: string;
  value: number;
  subtitle?: string;
  icon: typeof Users;
  loading: boolean;
  gradientIndex: number;
  href?: string;
}) {
  const gradient = gradientTiles[gradientIndex % gradientTiles.length];

  const content = (
    <>
      <div
        className={cn(
          "mb-3 inline-flex w-fit rounded-xl bg-gradient-to-br p-2.5 text-white shadow-brand-glow",
          gradient,
        )}
      >
        <Icon className="size-5" aria-hidden />
      </div>
      <p className="text-sm font-medium text-muted-foreground">{title}</p>
      {loading ? (
        <Skeleton className="mt-2 h-10 w-20" />
      ) : (
        <p className="mt-1 text-3xl font-bold tabular-nums tracking-tight text-foreground">{value}</p>
      )}
      {subtitle ? <p className="mt-1 text-xs text-muted-foreground">{subtitle}</p> : null}
      {href ? (
        <span className="mt-3 inline-flex items-center gap-1 text-xs font-semibold text-primary">
          View
          <ArrowUpRight className="size-3.5" />
        </span>
      ) : null}
    </>
  );

  const className = cn(
    "relative flex flex-col overflow-hidden rounded-2xl border border-border bg-card p-5 shadow-soft transition-all duration-300",
    href && "hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-soft-lg",
  );

  if (href) {
    return (
      <Link href={href} className={className}>
        {content}
      </Link>
    );
  }

  return <div className={className}>{content}</div>;
}

type ManagerTeamSummaryCardsProps = {
  summary: ManagerTeamSummary | undefined;
  loading: boolean;
};

export function ManagerTeamSummaryCards({ summary, loading }: ManagerTeamSummaryCardsProps) {
  const attendance = summary?.attendanceToday;

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
      <StatCard
        title="Active team members"
        value={summary?.activeEmployeeCount ?? 0}
        icon={Users}
        loading={loading}
        gradientIndex={0}
      />
      <StatCard
        title="Pending leave"
        value={summary?.pendingLeaveRequestCount ?? 0}
        icon={CalendarDays}
        loading={loading}
        gradientIndex={1}
        href="/leave"
      />
      <StatCard
        title="Present today"
        value={attendance?.presentCount ?? 0}
        subtitle={
          attendance
            ? `${attendance.absentCount} absent · ${attendance.onLeaveCount} on leave · ${attendance.checkedInOpenCount} checked in`
            : undefined
        }
        icon={UserCheck}
        loading={loading}
        gradientIndex={2}
        href="/attendance"
      />
      <StatCard
        title="Overdue tasks"
        value={summary?.overdueTaskCount ?? 0}
        icon={ListTodo}
        loading={loading}
        gradientIndex={3}
        href="/tasks"
      />
      <StatCard
        title="Upcoming changes"
        value={summary?.upcomingScheduledChangeCount ?? 0}
        icon={CalendarClock}
        loading={loading}
        gradientIndex={4}
        href="/employee-transfers"
      />
    </div>
  );
}

export function ManagerTeamQuickLinks() {
  const links = [
    { href: "/attendance", label: "Attendance", icon: Clock3 },
    { href: "/leave", label: "Leave", icon: CalendarDays },
    { href: "/tasks", label: "Tasks", icon: ListTodo },
  ] as const;

  return (
    <div className="flex flex-wrap gap-2">
      {links.map(({ href, label, icon: Icon }) => (
        <Link
          key={href}
          href={href}
          className="inline-flex items-center gap-2 rounded-lg border border-border bg-card px-3 py-2 text-sm font-medium text-foreground shadow-sm transition-colors hover:border-primary/30 hover:text-primary"
        >
          <Icon className="size-4" aria-hidden />
          {label}
        </Link>
      ))}
    </div>
  );
}

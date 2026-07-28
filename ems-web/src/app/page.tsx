import { HomeOverview } from "@/features/dashboard/components/HomeOverview";
import { OrganizationSetupLinks } from "@/features/organizations/components/OrganizationSetupLinks";
import Link from "next/link";

export default function Home() {
  return (
    <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-10">
      <section className="relative overflow-hidden rounded-3xl border border-border bg-card p-8 shadow-soft-lg md:p-10">
        <div
          className="pointer-events-none absolute -top-24 -right-24 size-64 rounded-full bg-brand-gradient opacity-20 blur-3xl"
          aria-hidden
        />
        <div
          className="pointer-events-none absolute -bottom-16 -left-16 size-48 rounded-full bg-brand-400 opacity-10 blur-2xl"
          aria-hidden
        />
        <div className="relative space-y-4">
          <p className="text-xs font-semibold uppercase tracking-widest text-primary">Dashboard</p>
          <h1 className="max-w-2xl text-3xl font-bold tracking-tight text-foreground md:text-4xl">
            Welcome back
            <span className="bg-brand-gradient bg-clip-text text-transparent"> — your team at a glance</span>
          </h1>
          <p className="max-w-2xl text-muted-foreground">
            Employee Management System — overview of your organization, people, and operations in one place.
          </p>
          <div className="flex flex-wrap gap-3 pt-2">
            <OrganizationSetupLinks />
            <Link
              href="/employee-portal"
              className="inline-flex items-center justify-center rounded-xl bg-brand-gradient px-5 py-2.5 text-sm font-semibold text-white shadow-brand-glow transition hover:brightness-110"
            >
              Employee portal
            </Link>
          </div>
        </div>
      </section>
      <HomeOverview />
    </main>
  );
}

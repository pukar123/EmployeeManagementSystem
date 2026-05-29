import { HomeOverview } from "@/features/dashboard/components/HomeOverview";
import { OrganizationSetupLinks } from "@/features/organizations/components/OrganizationSetupLinks";

import Link from "next/link";

export default function Home() {
  return (
    <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-10 px-4 py-10 sm:px-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Welcome back</h1>
        <p className="max-w-2xl text-muted-foreground">
          Employee Management System — overview of your organization at a glance.
        </p>
        <div className="flex flex-wrap gap-3 pt-1">
          <OrganizationSetupLinks />
          <Link
            href="/employee-portal"
            className="inline-flex items-center justify-center rounded-lg border border-primary/30 bg-primary/10 px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-primary/15 dark:border-primary/40 dark:hover:bg-primary/20"
          >
            Employee portal
          </Link>
        </div>
      </div>
      <HomeOverview />
    </main>
  );
}

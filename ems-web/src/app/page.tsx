import { HomeOverview } from "@/features/dashboard/components/HomeOverview";
import { OrganizationSetupLinks } from "@/features/organizations/components/OrganizationSetupLinks";

export default function Home() {
  return (
    <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-10 px-4 py-10 sm:px-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground">Welcome back</h1>
        <p className="max-w-2xl text-muted-foreground">
          Employee Management System — overview of your organization at a glance.
        </p>
        <OrganizationSetupLinks />
      </div>
      <HomeOverview />
    </main>
  );
}

import { EmployeeProfilePage } from "@/features/employees/components/EmployeeProfilePage";
import Link from "next/link";

type EmployeeDetailPageProps = {
  params: Promise<{ id: string }>;
};

export default async function EmployeeDetailPage({ params }: EmployeeDetailPageProps) {
  const { id } = await params;
  const employeeId = Number(id);

  if (!Number.isFinite(employeeId) || employeeId <= 0) {
    return (
      <main className="mx-auto max-w-6xl flex-1 px-4 py-10 sm:px-6">
        <div className="rounded-xl border border-border bg-card p-8 text-center">
          <p className="text-sm text-muted-foreground">Invalid employee link.</p>
          <Link href="/employees" className="mt-4 inline-block text-sm text-primary hover:underline">
            Back to employees
          </Link>
        </div>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-6xl flex-1 px-4 py-10 sm:px-6">
      <EmployeeProfilePage employeeId={employeeId} />
    </main>
  );
}

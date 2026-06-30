import { EmployeeProfilePage } from "@/features/employees/components/EmployeeProfilePage";

type EmployeeDetailPageProps = {
  params: Promise<{ id: string }>;
};

export default async function EmployeeDetailPage({ params }: EmployeeDetailPageProps) {
  const { id } = await params;
  const employeeId = Number(id);

  return (
    <main className="mx-auto max-w-6xl flex-1 px-4 py-10 sm:px-6">
      <EmployeeProfilePage employeeId={employeeId} />
    </main>
  );
}

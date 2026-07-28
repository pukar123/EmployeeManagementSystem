"use client";

import { useEmployeePortalEligibility } from "@/features/employee-portal/hooks";
import { LeaveSection } from "@/features/leave/components/LeaveSection";
import { Spinner } from "@/shared/components/Spinner";

export default function EmployeePortalLeavePage() {
  const eligibilityQuery = useEmployeePortalEligibility();

  if (eligibilityQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  const data = eligibilityQuery.data;
  if (!data?.hasLinkedEmployeeProfile || data.linkedEmployeeId == null || data.linkedOrganizationId == null) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10">
        <div
          className="rounded-xl border border-border bg-muted/30 px-4 py-5 text-sm text-muted-foreground"
          role="status"
        >
          <p className="font-medium text-foreground">Leave</p>
          <p className="mt-2 leading-relaxed">
            Personal leave requests are available when your sign-in account is linked to an employee record. Return to
            the employee portal home or contact an administrator if you need help.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl">
      <LeaveSection
        title="My leave"
        selfServiceEmployeeId={data.linkedEmployeeId}
        selfServiceOrganizationId={data.linkedOrganizationId}
      />
    </div>
  );
}

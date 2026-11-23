import { useEmployeePortalEligibility } from "@/features/employee-portal/hooks";

export type AttendanceAccess = {
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  /** Attendance admin (admin, or the attendance.admin menu): may pick any employee and add manual entries. */
  canManageOthers: boolean;
  linkedEmployeeId: number | null;
  linkedOrganizationId: number | null;
};

/**
 * What the current user may do on the attendance screens. The server enforces the same rule (B07); this only
 * decides what the UI offers, so staff without attendance admin see their own attendance and no employee picker.
 */
export function useAttendanceAccess(): AttendanceAccess {
  const eligibilityQuery = useEmployeePortalEligibility();
  const data = eligibilityQuery.data;

  return {
    isLoading: eligibilityQuery.isLoading,
    isError: eligibilityQuery.isError,
    error: eligibilityQuery.error,
    canManageOthers: data?.canManageOtherEmployeesAttendance ?? false,
    linkedEmployeeId: data?.linkedEmployeeId ?? null,
    linkedOrganizationId: data?.linkedOrganizationId ?? null,
  };
}

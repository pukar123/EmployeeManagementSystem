import { useEmployeePortalEligibility } from "@/features/employee-portal/hooks";

export type DocumentAccess = {
  isLoading: boolean;
  /** Document admin (admin, or the documents.admin menu): may list every document and upload, edit and delete. */
  canManageAll: boolean;
  linkedEmployeeId: number | null;
};

/**
 * What the current user may do with documents. The server enforces the same rule (B09); this only decides what the
 * UI offers, so people who are not document admins see no upload, edit or delete controls.
 */
export function useDocumentAccess(): DocumentAccess {
  const eligibilityQuery = useEmployeePortalEligibility();
  const data = eligibilityQuery.data;

  return {
    isLoading: eligibilityQuery.isLoading,
    canManageAll: data?.canManageAllDocuments ?? false,
    linkedEmployeeId: data?.linkedEmployeeId ?? null,
  };
}

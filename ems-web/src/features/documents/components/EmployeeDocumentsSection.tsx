"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { useDocumentMutations, useDocuments } from "../hooks";
import type { Document } from "../types/document.types";
import { DEFAULT_DOCUMENT_PAGE_SIZE, isForbiddenError } from "../utils/document-paging";
import { DeleteDocumentDialog } from "./DeleteDocumentDialog";
import { DocumentPager } from "./DocumentPager";
import { DocumentTable } from "./DocumentTable";
import { EditDocumentModal } from "./EditDocumentModal";
import { UploadDocumentModal } from "./UploadDocumentModal";

type EmployeeDocumentsSectionProps = {
  employeeId: number;
  /** Document admins only (`canManageAllDocuments`). Everyone else the server allows sees Download only. */
  canManage: boolean;
  enabled: boolean;
  title?: string;
  description?: string;
};

/**
 * One employee's documents, paged. The server decides who may see them (the employee, their direct manager and
 * document admins, B09); a refusal shows the permission message.
 */
export function EmployeeDocumentsSection({
  employeeId,
  canManage,
  enabled,
  title = "Documents",
  description = "Contracts, identification, certificates, and other files.",
}: EmployeeDocumentsSectionProps) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_DOCUMENT_PAGE_SIZE);
  const documentsQuery = useDocuments(employeeId, { page, pageSize }, enabled);
  const { download } = useDocumentMutations(employeeId);

  const [uploadOpen, setUploadOpen] = useState(false);
  const [editDoc, setEditDoc] = useState<Document | null>(null);
  const [deleteDoc, setDeleteDoc] = useState<Document | null>(null);
  const [downloadBusyId, setDownloadBusyId] = useState<number | null>(null);

  if (documentsQuery.isError && isForbiddenError(documentsQuery.error)) {
    return (
      <section className="rounded-xl border border-border p-6">
        <p className="text-sm text-muted-foreground">You do not have permission to view employee documents.</p>
      </section>
    );
  }

  const handleDownload = (doc: Document) => {
    setDownloadBusyId(doc.id);
    download.mutate(
      doc,
      {
        onSuccess: () => toast.success("Download started."),
        onError: (e) => toast.error(getErrorMessage(e)),
        onSettled: () => setDownloadBusyId(null),
      },
    );
  };

  const items = documentsQuery.data?.items ?? [];
  const totalPages = documentsQuery.data?.totalPages ?? 1;

  return (
    <section className="space-y-4 rounded-xl border border-border p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-foreground">{title}</h2>
          <p className="text-sm text-muted-foreground">{description}</p>
        </div>
        {canManage ? (
          <Button type="button" onClick={() => setUploadOpen(true)}>
            Upload document
          </Button>
        ) : null}
      </div>

      {documentsQuery.isLoading ? (
        <div className="flex justify-center py-12">
          <Spinner />
        </div>
      ) : documentsQuery.isError ? (
        <p className="text-sm text-destructive">{getErrorMessage(documentsQuery.error)}</p>
      ) : items.length === 0 && page === 1 ? (
        <div className="rounded-lg border border-dashed border-border py-10 text-center">
          <p className="text-sm text-muted-foreground">No documents uploaded yet.</p>
          {canManage ? (
            <Button type="button" className="mt-4" variant="secondary" onClick={() => setUploadOpen(true)}>
              Upload document
            </Button>
          ) : null}
        </div>
      ) : (
        <>
          <DocumentTable
            documents={items}
            canManage={canManage}
            downloadBusyId={downloadBusyId}
            onDownload={handleDownload}
            onEdit={(doc) => setEditDoc(doc)}
            onDelete={(doc) => setDeleteDoc(doc)}
          />
          <DocumentPager
            page={page}
            pageSize={pageSize}
            totalPages={totalPages}
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        </>
      )}

      {canManage ? (
        <>
          <UploadDocumentModal
            open={uploadOpen}
            employeeId={employeeId}
            onClose={() => setUploadOpen(false)}
            onUploaded={() => void documentsQuery.refetch()}
          />

          <EditDocumentModal
            open={editDoc != null}
            employeeId={employeeId}
            document={editDoc}
            onClose={() => setEditDoc(null)}
            onUpdated={() => void documentsQuery.refetch()}
          />

          <DeleteDocumentDialog
            open={deleteDoc != null}
            document={deleteDoc}
            employeeId={employeeId}
            onClose={() => setDeleteDoc(null)}
            onDeleted={() => void documentsQuery.refetch()}
          />
        </>
      ) : null}
    </section>
  );
}

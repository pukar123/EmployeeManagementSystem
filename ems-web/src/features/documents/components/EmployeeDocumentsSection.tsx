"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { getErrorMessage } from "@/shared/api/http-client";
import { useDocumentMutations, useDocuments } from "../hooks";
import type { Document } from "../types/document.types";
import { DeleteDocumentDialog } from "./DeleteDocumentDialog";
import { DocumentTable } from "./DocumentTable";
import { EditDocumentModal } from "./EditDocumentModal";
import { UploadDocumentModal } from "./UploadDocumentModal";

type EmployeeDocumentsSectionProps = {
  employeeId: number;
  canView: boolean;
  canManage: boolean;
  enabled: boolean;
};

export function EmployeeDocumentsSection({
  employeeId,
  canView,
  canManage,
  enabled,
}: EmployeeDocumentsSectionProps) {
  const documentsQuery = useDocuments(employeeId, enabled && canView);
  const { download } = useDocumentMutations(employeeId);

  const [uploadOpen, setUploadOpen] = useState(false);
  const [editDoc, setEditDoc] = useState<Document | null>(null);
  const [deleteDoc, setDeleteDoc] = useState<Document | null>(null);
  const [downloadBusyId, setDownloadBusyId] = useState<number | null>(null);

  if (!canView) {
    return (
      <section className="rounded-xl border border-border p-6">
        <p className="text-sm text-muted-foreground">You do not have permission to view employee documents.</p>
      </section>
    );
  }

  const handleDownload = (doc: Document) => {
    setDownloadBusyId(doc.id);
    download.mutate(
      { id: doc.id, fileName: doc.originalFileName },
      {
        onSuccess: () => toast.success("Download started."),
        onError: (e) => toast.error(getErrorMessage(e)),
        onSettled: () => setDownloadBusyId(null),
      },
    );
  };

  return (
    <section className="space-y-4 rounded-xl border border-border p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-foreground">Documents</h2>
          <p className="text-sm text-muted-foreground">Contracts, identification, certificates, and other files.</p>
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
      ) : (documentsQuery.data ?? []).length === 0 ? (
        <div className="rounded-lg border border-dashed border-border py-10 text-center">
          <p className="text-sm text-muted-foreground">No documents uploaded yet.</p>
          {canManage ? (
            <Button type="button" className="mt-4" variant="secondary" onClick={() => setUploadOpen(true)}>
              Upload document
            </Button>
          ) : null}
        </div>
      ) : (
        <DocumentTable
          documents={documentsQuery.data ?? []}
          canManage={canManage}
          downloadBusyId={downloadBusyId}
          onDownload={handleDownload}
          onEdit={(doc) => setEditDoc(doc)}
          onDelete={(doc) => setDeleteDoc(doc)}
        />
      )}

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
    </section>
  );
}

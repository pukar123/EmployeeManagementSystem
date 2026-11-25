"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import {
  DataTable,
  DataTableBody,
  DataTableCell,
  DataTableElement,
  DataTableHead,
  DataTableHeaderCell,
  DataTableRow,
} from "@/shared/components/DataTable";
import { getErrorMessage } from "@/shared/api/http-client";
import { cn } from "@/shared/utils/cn";
import { documentService } from "../services/documentService";
import { useAllDocuments, useDocumentAccess, useDocumentTypes } from "../hooks";
import type { Document } from "../types/document.types";
import { DEFAULT_DOCUMENT_PAGE_SIZE, isForbiddenError } from "../utils/document-paging";
import { formatDocumentDate, isPastDate } from "../utils/format-document-date";
import { DocumentPager } from "./DocumentPager";

/**
 * All documents, paged (B09). For document admins only (admin, or the documents.admin menu); the server refuses
 * anyone else. Read-only here: upload, edit and delete stay on each employee's profile Documents tab.
 */
export function DocumentsAdminPage() {
  const access = useDocumentAccess();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_DOCUMENT_PAGE_SIZE);
  const [documentTypeId, setDocumentTypeId] = useState<number | null>(null);
  const [downloadBusyId, setDownloadBusyId] = useState<number | null>(null);

  const typesQuery = useDocumentTypes(access.canManageAll);
  const documentsQuery = useAllDocuments({ page, pageSize, documentTypeId }, access.canManageAll);

  if (access.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (!access.canManageAll || (documentsQuery.isError && isForbiddenError(documentsQuery.error))) {
    return (
      <section className="rounded-xl border border-border p-6">
        <p className="text-sm text-muted-foreground">You do not have permission to view all documents.</p>
      </section>
    );
  }

  const handleDownload = async (doc: Document) => {
    setDownloadBusyId(doc.id);
    try {
      await documentService.download(doc);
      toast.success("Download started.");
    } catch (e) {
      toast.error(getErrorMessage(e));
    } finally {
      setDownloadBusyId(null);
    }
  };

  const items = documentsQuery.data?.items ?? [];
  const totalPages = documentsQuery.data?.totalPages ?? 1;

  return (
    <section className="space-y-4">
      <div className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Documents</h1>
        <p className="text-sm text-muted-foreground">
          Every employee document. To upload, edit or delete, open the employee&apos;s profile.
        </p>
      </div>

      <label className="flex w-fit items-center gap-2 text-sm text-muted-foreground">
        <span>Document type</span>
        <select
          className="rounded-lg border border-input bg-background px-2 py-1 text-sm"
          value={documentTypeId ?? ""}
          onChange={(e) => {
            setDocumentTypeId(e.target.value ? Number(e.target.value) : null);
            setPage(1);
          }}
        >
          <option value="">All types</option>
          {(typesQuery.data ?? []).map((type) => (
            <option key={type.id} value={type.id}>
              {type.name}
            </option>
          ))}
        </select>
      </label>

      {documentsQuery.isLoading ? (
        <div className="flex justify-center py-12">
          <Spinner />
        </div>
      ) : documentsQuery.isError ? (
        <p className="text-sm text-destructive">{getErrorMessage(documentsQuery.error)}</p>
      ) : (
        <>
          <div className={cn("overflow-x-auto", documentsQuery.isFetching && "opacity-70 transition-opacity")}>
            <DataTable isEmpty={items.length === 0} emptyMessage="No documents found.">
              <DataTableElement>
                <DataTableHead>
                  <tr>
                    <DataTableHeaderCell>Employee</DataTableHeaderCell>
                    <DataTableHeaderCell>Name</DataTableHeaderCell>
                    <DataTableHeaderCell>Type</DataTableHeaderCell>
                    <DataTableHeaderCell>Expiry</DataTableHeaderCell>
                    <DataTableHeaderCell>Updated</DataTableHeaderCell>
                    <DataTableHeaderCell>Download</DataTableHeaderCell>
                  </tr>
                </DataTableHead>
                <DataTableBody>
                  {items.map((doc) => (
                    <DataTableRow key={doc.id}>
                      <DataTableCell>{doc.employeeDisplayName ?? "Not assigned"}</DataTableCell>
                      <DataTableCell className="font-medium text-foreground">{doc.name}</DataTableCell>
                      <DataTableCell>{doc.documentTypeName}</DataTableCell>
                      <DataTableCell>
                        <span
                          className={cn(isPastDate(doc.expiryDate) && "font-medium text-destructive")}
                          title={isPastDate(doc.expiryDate) ? "Expired" : undefined}
                        >
                          {formatDocumentDate(doc.expiryDate)}
                        </span>
                      </DataTableCell>
                      <DataTableCell>{formatDocumentDate(doc.updatedAtUtc)}</DataTableCell>
                      <DataTableCell>
                        <Button
                          type="button"
                          variant="secondary"
                          size="sm"
                          disabled={downloadBusyId === doc.id}
                          onClick={() => void handleDownload(doc)}
                        >
                          {downloadBusyId === doc.id ? "Downloading…" : "Download"}
                        </Button>
                      </DataTableCell>
                    </DataTableRow>
                  ))}
                </DataTableBody>
              </DataTableElement>
            </DataTable>
          </div>
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
    </section>
  );
}
